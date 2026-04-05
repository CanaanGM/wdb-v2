using Api.Features.Auth.Contracts;

namespace Api.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthCommandResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthCommandResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthCommandResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<MeResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);

    Task<ForgotPasswordCommandResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        bool includeDebugResetToken,
        CancellationToken cancellationToken);

    Task<ResetPasswordCommandResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken);
}
