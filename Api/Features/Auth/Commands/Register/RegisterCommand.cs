using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Register;

public sealed record RegisterCommand(RegisterRequest Request) : ICommand<AuthCommandResult>;
