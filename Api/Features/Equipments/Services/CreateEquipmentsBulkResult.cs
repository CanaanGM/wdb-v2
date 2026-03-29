namespace Api.Features.Equipments.Services;

public enum CreateEquipmentsBulkResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2
}

public sealed class CreateEquipmentsBulkResult
{
    private CreateEquipmentsBulkResult(
        CreateEquipmentsBulkResultType resultType,
        int createdCount = 0,
        string? error = null)
    {
        ResultType = resultType;
        CreatedCount = createdCount;
        Error = error;
    }

    public CreateEquipmentsBulkResultType ResultType { get; }

    public int CreatedCount { get; }

    public string? Error { get; }

    public static CreateEquipmentsBulkResult Success(int createdCount) =>
        new(CreateEquipmentsBulkResultType.Success, createdCount);

    public static CreateEquipmentsBulkResult ValidationError(string error) =>
        new(CreateEquipmentsBulkResultType.ValidationError, error: error);

    public static CreateEquipmentsBulkResult Conflict(string error) =>
        new(CreateEquipmentsBulkResultType.Conflict, error: error);
}
