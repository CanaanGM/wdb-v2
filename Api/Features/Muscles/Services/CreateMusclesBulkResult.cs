namespace Api.Features.Muscles.Services;

public enum CreateMusclesBulkResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2
}

public sealed class CreateMusclesBulkResult
{
    private CreateMusclesBulkResult(
        CreateMusclesBulkResultType resultType,
        int createdCount = 0,
        string? error = null)
    {
        ResultType = resultType;
        CreatedCount = createdCount;
        Error = error;
    }

    public CreateMusclesBulkResultType ResultType { get; }

    public int CreatedCount { get; }

    public string? Error { get; }

    public static CreateMusclesBulkResult Success(int createdCount) =>
        new(CreateMusclesBulkResultType.Success, createdCount);

    public static CreateMusclesBulkResult ValidationError(string error) =>
        new(CreateMusclesBulkResultType.ValidationError, error: error);

    public static CreateMusclesBulkResult Conflict(string error) =>
        new(CreateMusclesBulkResultType.Conflict, error: error);
}

