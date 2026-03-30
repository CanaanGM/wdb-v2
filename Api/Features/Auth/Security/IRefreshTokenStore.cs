using Api.Features.Auth.Services;

namespace Api.Features.Auth.Security;

public interface IRefreshTokenStore
{
    Task<RefreshTokenEnvelope> IssueAsync(int userId, CancellationToken cancellationToken);

    Task<RefreshTokenRotationResult> RotateAsync(string currentToken, CancellationToken cancellationToken);

    Task RevokeAsync(string token, CancellationToken cancellationToken);
}

public sealed class RefreshTokenRotationResult
{
    private RefreshTokenRotationResult(
        bool succeeded,
        int? userId = null,
        RefreshTokenEnvelope? refreshToken = null)
    {
        Succeeded = succeeded;
        UserId = userId;
        RefreshToken = refreshToken;
    }

    public bool Succeeded { get; }

    public int? UserId { get; }

    public RefreshTokenEnvelope? RefreshToken { get; }

    public static RefreshTokenRotationResult Success(int userId, RefreshTokenEnvelope refreshToken) =>
        new(true, userId, refreshToken);

    public static RefreshTokenRotationResult Failed() => new(false);
}
