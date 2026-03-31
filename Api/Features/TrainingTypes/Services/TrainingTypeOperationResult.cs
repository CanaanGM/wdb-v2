namespace Api.Features.TrainingTypes.Services;

public enum TrainingTypeOperationResultType
{
    Success,
    NotFound,
    ValidationError,
    Conflict
}

public sealed class TrainingTypeOperationResult
{
    public TrainingTypeOperationResultType ResultType { get; init; }

    public string? Error { get; init; }

    public static TrainingTypeOperationResult Success() =>
        new() { ResultType = TrainingTypeOperationResultType.Success };

    public static TrainingTypeOperationResult NotFound(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.NotFound, Error = error };

    public static TrainingTypeOperationResult ValidationError(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.ValidationError, Error = error };

    public static TrainingTypeOperationResult Conflict(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.Conflict, Error = error };
}

public sealed class TrainingTypeOperationResult<T>
{
    public TrainingTypeOperationResultType ResultType { get; init; }

    public T? Value { get; init; }

    public string? Error { get; init; }

    public static TrainingTypeOperationResult<T> Success(T value) =>
        new() { ResultType = TrainingTypeOperationResultType.Success, Value = value };

    public static TrainingTypeOperationResult<T> NotFound(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.NotFound, Error = error };

    public static TrainingTypeOperationResult<T> ValidationError(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.ValidationError, Error = error };

    public static TrainingTypeOperationResult<T> Conflict(string error) =>
        new() { ResultType = TrainingTypeOperationResultType.Conflict, Error = error };
}
