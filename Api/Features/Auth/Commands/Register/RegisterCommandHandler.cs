using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(IAuthService authService)
    : ICommandHandler<RegisterCommand, AuthCommandResult>
{
    public async Task<AuthCommandResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        return await authService.RegisterAsync(command.Request, cancellationToken);
    }
}
