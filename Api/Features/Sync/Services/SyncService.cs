using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Features.Sync.Contracts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Sync.Services;

public sealed class SyncService(WorkoutLogDbContext dbContext) : ISyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<SyncBootstrapResponse> BootstrapAsync(int userId, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var mappings = await LoadEntityMappingsAsync(connection, cancellationToken);
        var response = new SyncBootstrapResponse();

        foreach (var mapping in mappings.Values)
        {
            var quotedTable = QuoteIdentifier(mapping.TableName);
            var whereSql = mapping.UserScoped
                ? "WHERE t.deleted_at_utc IS NULL AND t.user_id = @user_id"
                : "WHERE t.deleted_at_utc IS NULL";

            var sql = $"SELECT t.public_id, t.version, t.updated_at_utc, to_jsonb(t) FROM {quotedTable} t {whereSql} ORDER BY t.updated_at_utc, t.public_id";

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var payloadJson = GetJsonString(reader, 3);
                var payload = DeserializePayload(payloadJson);
                response.Changes.Add(new SyncChangeResponse
                {
                    Seq = 0,
                    EntityType = mapping.EntityType,
                    Action = "create",
                    EntityPublicId = reader.GetGuid(0),
                    Version = reader.GetInt64(1),
                    ChangedAtUtc = reader.GetDateTime(2),
                    Payload = payload,
                    DeletedAtUtc = null
                });
            }
        }

        response.Cursor = await GetCurrentCursorAsync(connection, userId, cancellationToken);
        return response;
    }

    public async Task<SyncPullResponse> PullAsync(int userId, long cursor, int limit, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        const string sql = """
            SELECT seq, entity_type, action, entity_public_id, version, changed_at_utc, payload, deleted_at_utc
            FROM sync_change_feed
            WHERE seq > @cursor
              AND (user_id IS NULL OR user_id = @user_id)
            ORDER BY seq ASC
            LIMIT @limit
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "cursor", cursor);
        AddParameter(command, "user_id", userId);
        AddParameter(command, "limit", limit);

        var response = new SyncPullResponse
        {
            Cursor = cursor,
            NextCursor = cursor,
            HasMore = false
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var seq = reader.GetInt64(0);
            var payloadJson = GetJsonString(reader, 6);
            var payload = DeserializePayload(payloadJson);

            response.Changes.Add(new SyncChangeResponse
            {
                Seq = seq,
                EntityType = reader.GetString(1),
                Action = reader.GetString(2),
                EntityPublicId = reader.GetGuid(3),
                Version = reader.GetInt64(4),
                ChangedAtUtc = reader.GetDateTime(5),
                Payload = payload,
                DeletedAtUtc = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            });

            response.NextCursor = seq;
        }

        response.HasMore = response.Changes.Count == limit;
        return response;
    }

    public async Task<SyncPushResponse> PushAsync(int userId, SyncPushRequest request, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var mappings = await LoadEntityMappingsAsync(connection, cancellationToken);

        var response = new SyncPushResponse();

        foreach (var operation in request.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cached = await TryReadCachedOperationResultAsync(connection, userId, operation.OpId, cancellationToken);
            if (cached is not null)
            {
                response.Results.Add(cached);
                continue;
            }

            var result = await ApplyOperationAsync(connection, mappings, userId, operation, cancellationToken);
            response.Results.Add(result);
            await PersistOperationResultAsync(connection, userId, operation.OpId, result, cancellationToken);
        }

        return response;
    }

    private async Task<SyncOperationResultResponse> ApplyOperationAsync(
        DbConnection connection,
        IReadOnlyDictionary<string, SyncEntityMapping> mappings,
        int userId,
        SyncOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!mappings.TryGetValue(operation.EntityType.Trim().ToLowerInvariant(), out var mapping))
        {
            return Rejected(operation.OpId, operation.EntityPublicId, "unknown_entity", "Entity type is not registered for sync.");
        }

        if (!mapping.PushEnabled)
        {
            return Rejected(operation.OpId, operation.EntityPublicId, "read_only_entity", "Entity type is pull-only.");
        }

        var action = operation.Action.Trim().ToLowerInvariant();
        if (action is not ("create" or "update" or "delete"))
        {
            return Rejected(operation.OpId, operation.EntityPublicId, "invalid_action", "Action must be create/update/delete.");
        }

        var resolvedPublicId = operation.EntityPublicId;
        if (resolvedPublicId is null && operation.Payload.TryGetValue("public_id", out var payloadPublicIdRaw))
        {
            if (TryReadGuid(payloadPublicIdRaw, out var payloadPublicId))
            {
                resolvedPublicId = payloadPublicId;
            }
        }

        if (action != "create" && resolvedPublicId is null)
        {
            return Rejected(operation.OpId, null, "missing_public_id", "entityPublicId is required for update/delete.");
        }

        var current = resolvedPublicId is null
            ? null
            : await GetCurrentRowAsync(connection, mapping, resolvedPublicId.Value, userId, cancellationToken);

        if (action == "create")
        {
            resolvedPublicId ??= Guid.NewGuid();
            if (current is not null)
            {
                return Conflict(operation.OpId, current.Value.PublicId, current.Value.Version, current.Value.Payload, "Entity already exists.");
            }

            var mergedPayload = ToJsonObject(operation.Payload);
            mergedPayload["public_id"] = JsonValue.Create(resolvedPublicId);
            if (mapping.UserScoped)
            {
                mergedPayload["user_id"] = JsonValue.Create(userId);
            }

            try
            {
                var upserted = await UpsertRowAsync(connection, mapping, mergedPayload, cancellationToken);
                return Applied(operation.OpId, upserted.PublicId, upserted.Version, upserted.Payload);
            }
            catch (DbException)
            {
                return Rejected(operation.OpId, resolvedPublicId, "invalid_payload", "Payload does not satisfy table constraints.");
            }
        }

        if (current is null)
        {
            return Rejected(operation.OpId, resolvedPublicId, "not_found", "Entity was not found.");
        }

        if (operation.BaseVersion.HasValue && operation.BaseVersion.Value != current.Value.Version)
        {
            return Conflict(operation.OpId, current.Value.PublicId, current.Value.Version, current.Value.Payload, "Server version does not match baseVersion.");
        }

        if (action == "delete")
        {
            var deleted = await SoftDeleteRowAsync(connection, mapping, current.Value.PublicId, userId, cancellationToken);
            if (deleted is null)
            {
                return Rejected(operation.OpId, current.Value.PublicId, "not_found", "Entity was not found.");
            }

            return Applied(operation.OpId, deleted.Value.PublicId, deleted.Value.Version, deleted.Value.Payload);
        }

        var merged = MergePayload(current.Value.Payload, operation.Payload);
        merged["public_id"] = JsonValue.Create(current.Value.PublicId);
        if (mapping.UserScoped)
        {
            merged["user_id"] = JsonValue.Create(userId);
        }

        try
        {
            var updated = await UpsertRowAsync(connection, mapping, merged, cancellationToken);
            return Applied(operation.OpId, updated.PublicId, updated.Version, updated.Payload);
        }
        catch (DbException)
        {
            return Rejected(operation.OpId, current.Value.PublicId, "invalid_payload", "Payload does not satisfy table constraints.");
        }
    }

    private async Task<Dictionary<string, SyncEntityMapping>> LoadEntityMappingsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT entity_type, table_name, user_scoped, push_enabled FROM sync_entity_registry";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var mappings = new Dictionary<string, SyncEntityMapping>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var mapping = new SyncEntityMapping(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3));

            mappings[mapping.EntityType.ToLowerInvariant()] = mapping;
        }

        return mappings;
    }

    private static async Task<long> GetCurrentCursorAsync(DbConnection connection, int userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(MAX(seq), 0) FROM sync_change_feed WHERE user_id IS NULL OR user_id = @user_id";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "user_id", userId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }

    private async Task<CurrentRow?> GetCurrentRowAsync(
        DbConnection connection,
        SyncEntityMapping mapping,
        Guid publicId,
        int userId,
        CancellationToken cancellationToken)
    {
        var quotedTable = QuoteIdentifier(mapping.TableName);
        var whereSql = mapping.UserScoped
            ? "WHERE t.public_id = @public_id AND t.user_id = @user_id"
            : "WHERE t.public_id = @public_id";

        var sql = $"SELECT t.public_id, t.version, to_jsonb(t) FROM {quotedTable} t {whereSql} LIMIT 1";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "public_id", publicId);
        AddParameter(command, "user_id", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var payloadJson = GetJsonString(reader, 2);
        return new CurrentRow(
            reader.GetGuid(0),
            reader.GetInt64(1),
            DeserializePayload(payloadJson),
            payloadJson);
    }

    private async Task<UpsertResult> UpsertRowAsync(
        DbConnection connection,
        SyncEntityMapping mapping,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        var columns = await GetTableColumnsAsync(connection, mapping.TableName, cancellationToken);
        var payloadColumns = payload
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var insertColumns = columns
            .Where(column => !string.Equals(column, "id", StringComparison.OrdinalIgnoreCase)
                             && payloadColumns.Contains(column))
            .ToList();

        var updateColumns = insertColumns
            .Where(column => column is not ("id" or "public_id" or "created_at_utc" or "user_id" or "version" or "updated_at_utc"))
            .ToList();

        if (insertColumns.Count == 0)
        {
            throw new InvalidOperationException("No insertable columns were provided in payload.");
        }

        var quotedTable = QuoteIdentifier(mapping.TableName);
        var columnListSql = string.Join(", ", insertColumns.Select(QuoteIdentifier));
        var updateSetSql = updateColumns.Count == 0
            ? "updated_at_utc = now()"
            : string.Join(", ", updateColumns.Select(column => $"{QuoteIdentifier(column)} = EXCLUDED.{QuoteIdentifier(column)}"));

        var sql = $"""
            INSERT INTO {quotedTable} AS t ({columnListSql})
            SELECT {columnListSql}
            FROM jsonb_populate_record(NULL::{quotedTable}, @payload::jsonb)
            ON CONFLICT (public_id)
            DO UPDATE SET {updateSetSql}
            RETURNING t.public_id, t.version, to_jsonb(t)
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "payload", payload.ToJsonString(JsonOptions));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Upsert failed.");
        }

        var payloadJson = GetJsonString(reader, 2);
        return new UpsertResult(
            reader.GetGuid(0),
            reader.GetInt64(1),
            DeserializePayload(payloadJson));
    }

    private static async Task<UpsertResult?> SoftDeleteRowAsync(
        DbConnection connection,
        SyncEntityMapping mapping,
        Guid publicId,
        int userId,
        CancellationToken cancellationToken)
    {
        var quotedTable = QuoteIdentifier(mapping.TableName);
        var whereSql = mapping.UserScoped
            ? "public_id = @public_id AND user_id = @user_id"
            : "public_id = @public_id";

        var sql = $"""
            UPDATE {quotedTable} AS t
            SET deleted_at_utc = now()
            WHERE {whereSql}
            RETURNING t.public_id, t.version, to_jsonb(t)
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "public_id", publicId);
        AddParameter(command, "user_id", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var payloadJson = GetJsonString(reader, 2);
        return new UpsertResult(
            reader.GetGuid(0),
            reader.GetInt64(1),
            DeserializePayload(payloadJson));
    }

    private static async Task<SyncOperationResultResponse?> TryReadCachedOperationResultAsync(
        DbConnection connection,
        int userId,
        Guid opId,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT result_json FROM sync_push_operation WHERE user_id = @user_id AND op_id = @op_id LIMIT 1";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "user_id", userId);
        AddParameter(command, "op_id", opId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull)
        {
            return null;
        }

        var json = value.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SyncOperationResultResponse>(json, JsonOptions);
    }

    private static async Task PersistOperationResultAsync(
        DbConnection connection,
        int userId,
        Guid opId,
        SyncOperationResultResponse result,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sync_push_operation (op_id, user_id, result_json, created_at_utc)
            VALUES (@op_id, @user_id, @result_json::jsonb, now())
            ON CONFLICT (op_id) DO NOTHING
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "op_id", opId);
        AddParameter(command, "user_id", userId);
        AddParameter(command, "result_json", JsonSerializer.Serialize(result, JsonOptions));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SyncOperationResultResponse Applied(
        Guid opId,
        Guid publicId,
        long version,
        Dictionary<string, object?> payload)
    {
        return new SyncOperationResultResponse
        {
            OpId = opId,
            Status = "applied",
            EntityPublicId = publicId,
            ServerVersion = version,
            CanonicalPayload = payload
        };
    }

    private static SyncOperationResultResponse Conflict(
        Guid opId,
        Guid publicId,
        long version,
        Dictionary<string, object?> payload,
        string reason)
    {
        return new SyncOperationResultResponse
        {
            OpId = opId,
            Status = "conflict",
            EntityPublicId = publicId,
            ServerVersion = version,
            CanonicalPayload = payload,
            ConflictReason = reason,
            ErrorCode = "version_conflict"
        };
    }

    private static SyncOperationResultResponse Rejected(
        Guid opId,
        Guid? publicId,
        string errorCode,
        string message)
    {
        return new SyncOperationResultResponse
        {
            OpId = opId,
            Status = "rejected",
            EntityPublicId = publicId,
            ErrorCode = errorCode,
            ConflictReason = message
        };
    }

    private static async Task<List<string>> GetTableColumnsAsync(
        DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @table_name
            ORDER BY ordinal_position
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "table_name", tableName);

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"No columns found for table '{tableName}'.");
        }

        return columns;
    }

    private static JsonObject MergePayload(
        Dictionary<string, object?> existing,
        Dictionary<string, object?> incoming)
    {
        var merged = ToJsonObject(existing);
        foreach (var pair in incoming)
        {
            merged[pair.Key] = pair.Value is null
                ? null
                : JsonSerializer.SerializeToNode(pair.Value, JsonOptions);
        }

        return merged;
    }

    private static JsonObject ToJsonObject(Dictionary<string, object?> payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var node = JsonNode.Parse(json) as JsonObject;
        return node ?? [];
    }

    private static Dictionary<string, object?> ToDictionary(JsonObject payload)
    {
        var json = payload.ToJsonString(JsonOptions);
        return DeserializePayload(json);
    }

    private static Dictionary<string, object?> DeserializePayload(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions)
               ?? [];
    }

    private static bool TryReadGuid(object? raw, out Guid value)
    {
        if (raw is Guid guid)
        {
            value = guid;
            return true;
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String
                && Guid.TryParse(element.GetString(), out var parsedFromElement))
            {
                value = parsedFromElement;
                return true;
            }
        }

        if (raw is string text && Guid.TryParse(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = Guid.Empty;
        return false;
    }

    private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private static string GetJsonString(DbDataReader reader, int ordinal)
    {
        return reader.GetValue(ordinal).ToString() ?? "{}";
    }

    private readonly record struct SyncEntityMapping(string EntityType, string TableName, bool UserScoped, bool PushEnabled);

    private readonly record struct CurrentRow(
        Guid PublicId,
        long Version,
        Dictionary<string, object?> Payload,
        string PayloadJson);

    private readonly record struct UpsertResult(
        Guid PublicId,
        long Version,
        Dictionary<string, object?> Payload);
}
