using Api.Application.Cqrs;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<AuthCommandResult>;
