using Api.Features.Auth.Contracts;

namespace Api.Features.Auth.Services;

public enum AuthCommandResultType
{
    Success = 0,
    ValidationError = 1,
    Conflict = 2,
    Unauthorized = 3
}

public sealed class AuthCommandResult
{
    private AuthCommandResult(
        AuthCommandResultType resultType,
        AuthResponse? response = null,
        RefreshTokenEnvelope? refreshToken = null,
        string? error = null)
    {
        ResultType = resultType;
        Response = response;
        RefreshToken = refreshToken;
        Error = error;
    }

    public AuthCommandResultType ResultType { get; }

    public AuthResponse? Response { get; }

    public RefreshTokenEnvelope? RefreshToken { get; }

    public string? Error { get; }

    public static AuthCommandResult Success(AuthResponse response, RefreshTokenEnvelope refreshToken) =>
        new(AuthCommandResultType.Success, response, refreshToken);

    public static AuthCommandResult ValidationError(string error) =>
        new(AuthCommandResultType.ValidationError, error: error);

    public static AuthCommandResult Conflict(string error) =>
        new(AuthCommandResultType.Conflict, error: error);

    public static AuthCommandResult Unauthorized(string error) =>
        new(AuthCommandResultType.Unauthorized, error: error);
}

public sealed class RefreshTokenEnvelope
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }
}
