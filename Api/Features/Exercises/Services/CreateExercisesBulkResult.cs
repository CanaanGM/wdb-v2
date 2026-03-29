namespace Api.Features.Exercises.Services;

public enum CreateExercisesBulkResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2
}

public sealed class CreateExercisesBulkResult
{
    private CreateExercisesBulkResult(
        CreateExercisesBulkResultType resultType,
        int createdCount = 0,
        string? error = null)
    {
        ResultType = resultType;
        CreatedCount = createdCount;
        Error = error;
    }

    public CreateExercisesBulkResultType ResultType { get; }

    public int CreatedCount { get; }

    public string? Error { get; }

    public static CreateExercisesBulkResult Success(int createdCount) =>
        new(CreateExercisesBulkResultType.Success, createdCount);

    public static CreateExercisesBulkResult ValidationError(string error) =>
        new(CreateExercisesBulkResultType.ValidationError, error: error);

    public static CreateExercisesBulkResult Conflict(string error) =>
        new(CreateExercisesBulkResultType.Conflict, error: error);
}

