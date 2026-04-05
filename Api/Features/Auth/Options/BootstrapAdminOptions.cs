namespace Api.Features.Auth.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "Auth:BootstrapAdmin";
    public const string DefaultPassword = "CHANGE_ME_TO_A_STRONG_BOOTSTRAP_ADMIN_PASSWORD";

    public bool Enabled { get; set; }

    public string Email { get; set; } = "admin@workoutlog.local";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = DefaultPassword;
}
