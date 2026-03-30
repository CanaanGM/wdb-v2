namespace Api.Features.WorkoutBlocks.Services;

public enum WorkoutBlockOperationResultType
{
    Success = 0,
    ValidationError = 1,
    NotFound = 2
}

public sealed class WorkoutBlockOperationResult<T>
{
    private WorkoutBlockOperationResult(WorkoutBlockOperationResultType resultType, T? value = default, string? error = null)
    {
        ResultType = resultType;
        Value = value;
        Error = error;
    }

    public WorkoutBlockOperationResultType ResultType { get; }

    public T? Value { get; }

    public string? Error { get; }

    public static WorkoutBlockOperationResult<T> Success(T value) =>
        new(WorkoutBlockOperationResultType.Success, value);

    public static WorkoutBlockOperationResult<T> ValidationError(string error) =>
        new(WorkoutBlockOperationResultType.ValidationError, error: error);

    public static WorkoutBlockOperationResult<T> NotFound(string error) =>
        new(WorkoutBlockOperationResultType.NotFound, error: error);
}

public sealed class WorkoutBlockOperationResult
{
    private WorkoutBlockOperationResult(WorkoutBlockOperationResultType resultType, string? error = null)
    {
        ResultType = resultType;
        Error = error;
    }

    public WorkoutBlockOperationResultType ResultType { get; }

    public string? Error { get; }

    public static WorkoutBlockOperationResult Success() => new(WorkoutBlockOperationResultType.Success);

    public static WorkoutBlockOperationResult ValidationError(string error) =>
        new(WorkoutBlockOperationResultType.ValidationError, error);

    public static WorkoutBlockOperationResult NotFound(string error) =>
        new(WorkoutBlockOperationResultType.NotFound, error);
}
