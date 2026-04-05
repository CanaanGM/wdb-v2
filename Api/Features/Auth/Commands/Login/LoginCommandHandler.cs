using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(IAuthService authService)
    : ICommandHandler<LoginCommand, AuthCommandResult>
{
    public async Task<AuthCommandResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(command.Request, cancellationToken);
    }
}
