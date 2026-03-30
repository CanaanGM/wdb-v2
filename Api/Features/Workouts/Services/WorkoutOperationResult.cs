namespace Api.Features.Workouts.Services;

public enum WorkoutOperationResultType
{
    Success = 0,
    ValidationError = 1,
    NotFound = 2
}

public sealed class WorkoutOperationResult<T>
{
    private WorkoutOperationResult(WorkoutOperationResultType resultType, T? value = default, string? error = null)
    {
        ResultType = resultType;
        Value = value;
        Error = error;
    }

    public WorkoutOperationResultType ResultType { get; }

    public T? Value { get; }

    public string? Error { get; }

    public static WorkoutOperationResult<T> Success(T value) =>
        new(WorkoutOperationResultType.Success, value);

    public static WorkoutOperationResult<T> ValidationError(string error) =>
        new(WorkoutOperationResultType.ValidationError, error: error);

    public static WorkoutOperationResult<T> NotFound(string error) =>
        new(WorkoutOperationResultType.NotFound, error: error);
}

public sealed class WorkoutOperationResult
{
    private WorkoutOperationResult(WorkoutOperationResultType resultType, string? error = null)
    {
        ResultType = resultType;
        Error = error;
    }

    public WorkoutOperationResultType ResultType { get; }

    public string? Error { get; }

    public static WorkoutOperationResult Success() => new(WorkoutOperationResultType.Success);

    public static WorkoutOperationResult ValidationError(string error) =>
        new(WorkoutOperationResultType.ValidationError, error);

    public static WorkoutOperationResult NotFound(string error) =>
        new(WorkoutOperationResultType.NotFound, error);
}
