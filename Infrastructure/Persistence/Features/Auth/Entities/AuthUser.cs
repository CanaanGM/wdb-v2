using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Features.Auth.Entities;

public sealed class AuthUser : IdentityUser<int>
{
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<RefreshSession> RefreshSessions { get; set; } = new List<RefreshSession>();
}
