namespace Api.Features.Auth.Options;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Auth:RefreshToken";

    public string CookieName { get; set; } = "refreshToken";

    public int RefreshTokenDays { get; set; } = 7;

    public string CookiePath { get; set; } = "/api/auth";
}
