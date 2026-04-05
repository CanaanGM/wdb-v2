namespace Api.Features.Auth.Contracts;

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = string.Empty;

    public string? DebugResetToken { get; init; }
}
