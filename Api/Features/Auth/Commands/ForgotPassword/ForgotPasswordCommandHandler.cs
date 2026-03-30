using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(IAuthService authService)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordCommandResult>
{
    public async Task<ForgotPasswordCommandResult> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return await authService.ForgotPasswordAsync(
            command.Request,
            command.IncludeDebugResetToken,
            cancellationToken);
    }
}
