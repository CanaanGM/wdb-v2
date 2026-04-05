namespace Api.Features.Sync.Contracts;

public sealed class SyncPushRequest
{
    public string DeviceId { get; set; } = string.Empty;

    public List<SyncOperationRequest> Operations { get; set; } = [];
}

public sealed class SyncOperationRequest
{
    public Guid OpId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public Guid? EntityPublicId { get; set; }

    public long? BaseVersion { get; set; }

    public Dictionary<string, object?> Payload { get; set; } = [];

    public DateTime OccurredAtUtc { get; set; }
}

public sealed class SyncPushResponse
{
    public List<SyncOperationResultResponse> Results { get; set; } = [];
}

public sealed class SyncOperationResultResponse
{
    public Guid OpId { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? EntityPublicId { get; set; }

    public long? ServerVersion { get; set; }

    public Dictionary<string, object?>? CanonicalPayload { get; set; }

    public string? ConflictReason { get; set; }

    public string? ErrorCode { get; set; }
}

public sealed class SyncPullResponse
{
    public long Cursor { get; set; }

    public long NextCursor { get; set; }

    public bool HasMore { get; set; }

    public List<SyncChangeResponse> Changes { get; set; } = [];
}

public sealed class SyncChangeResponse
{
    public long Seq { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public Guid EntityPublicId { get; set; }

    public long Version { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public Dictionary<string, object?> Payload { get; set; } = [];

    public DateTime? DeletedAtUtc { get; set; }
}

public sealed class SyncBootstrapResponse
{
    public long Cursor { get; set; }

    public List<SyncChangeResponse> Changes { get; set; } = [];
}
