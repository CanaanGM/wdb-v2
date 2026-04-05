using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    ForgotPasswordRequest Request,
    bool IncludeDebugResetToken) : ICommand<ForgotPasswordCommandResult>;
