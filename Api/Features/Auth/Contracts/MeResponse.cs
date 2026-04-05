namespace Api.Features.Auth.Contracts;

public sealed class MeResponse
{
    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = [];
}
