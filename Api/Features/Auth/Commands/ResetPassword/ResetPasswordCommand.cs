using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : ICommand<ResetPasswordCommandResult>;
