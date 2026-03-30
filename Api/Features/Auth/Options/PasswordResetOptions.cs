namespace Api.Features.Auth.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "Auth:PasswordReset";

    public bool IncludeDebugResetToken { get; set; }
}
