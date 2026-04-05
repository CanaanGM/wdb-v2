namespace Api.Features.Auth.Security;

public sealed class AccessTokenEnvelope
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }
}
