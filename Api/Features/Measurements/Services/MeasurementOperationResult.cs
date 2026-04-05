namespace Api.Features.Measurements.Services;

public enum MeasurementOperationResultType
{
    Success,
    NotFound,
    ValidationError
}

public sealed class MeasurementOperationResult
{
    public MeasurementOperationResultType ResultType { get; init; }

    public string? Error { get; init; }

    public static MeasurementOperationResult Success() =>
        new() { ResultType = MeasurementOperationResultType.Success };

    public static MeasurementOperationResult NotFound(string error) =>
        new() { ResultType = MeasurementOperationResultType.NotFound, Error = error };

    public static MeasurementOperationResult ValidationError(string error) =>
        new() { ResultType = MeasurementOperationResultType.ValidationError, Error = error };
}

public sealed class MeasurementOperationResult<T>
{
    public MeasurementOperationResultType ResultType { get; init; }

    public T? Value { get; init; }

    public string? Error { get; init; }

    public static MeasurementOperationResult<T> Success(T value) =>
        new() { ResultType = MeasurementOperationResultType.Success, Value = value };

    public static MeasurementOperationResult<T> NotFound(string error) =>
        new() { ResultType = MeasurementOperationResultType.NotFound, Error = error };

    public static MeasurementOperationResult<T> ValidationError(string error) =>
        new() { ResultType = MeasurementOperationResultType.ValidationError, Error = error };
}
