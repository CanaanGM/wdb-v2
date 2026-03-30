using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(IAuthService authService)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordCommandResult>
{
    public async Task<ResetPasswordCommandResult> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return await authService.ResetPasswordAsync(command.Request, cancellationToken);
    }
}
