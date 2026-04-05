using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Login;

public sealed record LoginCommand(LoginRequest Request) : ICommand<AuthCommandResult>;
