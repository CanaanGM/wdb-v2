namespace Api.Features.Plans.Services;

public enum PlanOperationResultType
{
    Success = 0,
    ValidationError = 1,
    NotFound = 2
}

public sealed class PlanOperationResult<T>
{
    private PlanOperationResult(PlanOperationResultType resultType, T? value = default, string? error = null)
    {
        ResultType = resultType;
        Value = value;
        Error = error;
    }

    public PlanOperationResultType ResultType { get; }

    public T? Value { get; }

    public string? Error { get; }

    public static PlanOperationResult<T> Success(T value) =>
        new(PlanOperationResultType.Success, value);

    public static PlanOperationResult<T> ValidationError(string error) =>
        new(PlanOperationResultType.ValidationError, error: error);

    public static PlanOperationResult<T> NotFound(string error) =>
        new(PlanOperationResultType.NotFound, error: error);
}

public sealed class PlanOperationResult
{
    private PlanOperationResult(PlanOperationResultType resultType, string? error = null)
    {
        ResultType = resultType;
        Error = error;
    }

    public PlanOperationResultType ResultType { get; }

    public string? Error { get; }

    public static PlanOperationResult Success() => new(PlanOperationResultType.Success);

    public static PlanOperationResult ValidationError(string error) =>
        new(PlanOperationResultType.ValidationError, error);

    public static PlanOperationResult NotFound(string error) =>
        new(PlanOperationResultType.NotFound, error);
}
