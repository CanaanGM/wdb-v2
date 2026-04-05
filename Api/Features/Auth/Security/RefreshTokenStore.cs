using System.Security.Cryptography;
using Api.Features.Auth.Options;
using Api.Features.Auth.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.Auth.Security;

public sealed class RefreshTokenStore(
    WorkoutLogDbContext dbContext,
    IOptions<RefreshTokenOptions> refreshTokenOptions) : IRefreshTokenStore
{
    private readonly RefreshTokenOptions _refreshTokenOptions = refreshTokenOptions.Value;

    public async Task<RefreshTokenEnvelope> IssueAsync(int userId, CancellationToken cancellationToken)
    {
        var token = GenerateToken();
        var tokenHash = RefreshTokenHasher.Hash(token);
        var expiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenOptions.RefreshTokenDays);

        var refreshSession = new RefreshSession
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc
        };

        dbContext.RefreshSessions.Add(refreshSession);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenEnvelope
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(string currentToken, CancellationToken cancellationToken)
    {
        var currentTokenHash = RefreshTokenHasher.Hash(currentToken);
        var now = DateTime.UtcNow;

        var currentSession = await dbContext.RefreshSessions
            .AsNoTracking()
            .Where(x => x.TokenHash == currentTokenHash)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.ExpiresAtUtc,
                x.RevokedAtUtc,
                x.ReplacedByHash
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (currentSession is null)
        {
            return RefreshTokenRotationResult.Failed();
        }

        if (currentSession.RevokedAtUtc.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(currentSession.ReplacedByHash))
            {
                await RevokeAllForUserAsync(currentSession.UserId, cancellationToken);
            }

            return RefreshTokenRotationResult.Failed();
        }

        if (currentSession.ExpiresAtUtc <= now)
        {
            return RefreshTokenRotationResult.Failed();
        }

        var newToken = GenerateToken();
        var newTokenHash = RefreshTokenHasher.Hash(newToken);
        var newTokenExpiresAtUtc = now.AddDays(_refreshTokenOptions.RefreshTokenDays);

        var updatedRows = await dbContext.RefreshSessions
            .Where(x => x.Id == currentSession.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAtUtc, now)
                    .SetProperty(x => x.ReplacedByHash, newTokenHash),
                cancellationToken);

        if (updatedRows == 0)
        {
            return RefreshTokenRotationResult.Failed();
        }

        dbContext.RefreshSessions.Add(new RefreshSession
        {
            UserId = currentSession.UserId,
            TokenHash = newTokenHash,
            ExpiresAtUtc = newTokenExpiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return RefreshTokenRotationResult.Success(
            currentSession.UserId,
            new RefreshTokenEnvelope
            {
                Token = newToken,
                ExpiresAtUtc = newTokenExpiresAtUtc
            });
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHasher.Hash(token);
        var now = DateTime.UtcNow;

        await dbContext.RefreshSessions
            .Where(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await dbContext.RefreshSessions
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                cancellationToken);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
