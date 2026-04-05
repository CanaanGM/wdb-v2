namespace Api.Features.Auth.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";
    public const string DefaultSecret = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_WITH_AT_LEAST_32_CHARACTERS";

    public string Issuer { get; set; } = "WorkoutLog";

    public string Audience { get; set; } = "WorkoutLog.Client";

    public string Secret { get; set; } = DefaultSecret;

    public int AccessTokenMinutes { get; set; } = 15;
}
