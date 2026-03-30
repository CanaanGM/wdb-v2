using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler(IAuthService authService)
    : ICommandHandler<RefreshCommand, AuthCommandResult>
{
    public async Task<AuthCommandResult> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        return await authService.RefreshAsync(command.RefreshToken, cancellationToken);
    }
}
