namespace Api.Features.Auth.Services;

public enum ResetPasswordCommandResultType
{
    Success = 0,
    ValidationError = 1
}

public sealed class ResetPasswordCommandResult
{
    private ResetPasswordCommandResult(ResetPasswordCommandResultType resultType, string? error = null)
    {
        ResultType = resultType;
        Error = error;
    }

    public ResetPasswordCommandResultType ResultType { get; }

    public string? Error { get; }

    public static ResetPasswordCommandResult Success() =>
        new(ResetPasswordCommandResultType.Success);

    public static ResetPasswordCommandResult ValidationError(string error) =>
        new(ResetPasswordCommandResultType.ValidationError, error);
}
