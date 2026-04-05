using Api.Application.Cqrs;

namespace Api.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<bool>;
