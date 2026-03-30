namespace Api.Features.Auth.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "Auth:BootstrapAdmin";

    public bool Enabled { get; set; }

    public string Email { get; set; } = "admin@workoutlog.local";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "ChangeMe123!";
}
