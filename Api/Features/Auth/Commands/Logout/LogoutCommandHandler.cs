using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IAuthService authService)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(command.RefreshToken, cancellationToken);
        return true;
    }
}
