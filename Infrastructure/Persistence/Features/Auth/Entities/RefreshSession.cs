namespace Infrastructure.Persistence.Features.Auth.Entities;

public sealed class RefreshSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByHash { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public AuthUser User { get; set; } = null!;
}
