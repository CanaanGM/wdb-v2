using Api.Features.Auth.Contracts;

namespace Api.Features.Auth.Services;

public sealed class ForgotPasswordCommandResult
{
    public ForgotPasswordResponse Response { get; init; } = new();
}
