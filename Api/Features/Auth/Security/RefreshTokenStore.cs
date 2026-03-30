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
            .SingleOrDefaultAsync(x => x.TokenHash == currentTokenHash, cancellationToken);

        if (currentSession is null || currentSession.RevokedAtUtc.HasValue || currentSession.ExpiresAtUtc <= now)
        {
            return RefreshTokenRotationResult.Failed();
        }

        var newToken = GenerateToken();
        var newTokenHash = RefreshTokenHasher.Hash(newToken);
        var newTokenExpiresAtUtc = now.AddDays(_refreshTokenOptions.RefreshTokenDays);

        currentSession.RevokedAtUtc = now;
        currentSession.ReplacedByHash = newTokenHash;

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
        var refreshSession = await dbContext.RefreshSessions
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshSession is null || refreshSession.RevokedAtUtc.HasValue)
        {
            return;
        }

        refreshSession.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
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
