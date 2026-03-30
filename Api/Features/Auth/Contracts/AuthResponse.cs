namespace Api.Features.Auth.Contracts;

public sealed class AuthResponse
{
    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = [];

    public string AccessToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; init; }
}
