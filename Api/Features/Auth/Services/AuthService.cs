using Api.Application.Text;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Security;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Auth.Services;

public sealed class AuthService(
    UserManager<AuthUser> userManager,
    RoleManager<AuthRole> roleManager,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore) : IAuthService
{
    private const string GenericAuthFailureMessage = "Invalid credentials.";
    private const string GenericForgotPasswordMessage = "If the account exists, reset instructions were generated.";
    private const string GenericResetPasswordFailureMessage = "Unable to reset password with the provided token.";

    public async Task<AuthCommandResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedUsername = StorageTextNormalizer.NormalizeKey(request.Username);
        var normalizedEmail = StorageTextNormalizer.NormalizeKey(request.Email);

        var user = new AuthUser
        {
            UserName = normalizedUsername,
            Email = normalizedEmail
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return MapIdentityFailure(createResult.Errors);
        }

        await EnsureRolesExistAsync(cancellationToken);

        var addToRoleResult = await userManager.AddToRoleAsync(user, AuthRoles.User);
        if (!addToRoleResult.Succeeded)
        {
            return AuthCommandResult.ValidationError(string.Join("; ", addToRoleResult.Errors.Select(x => x.Description)));
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = await refreshTokenStore.IssueAsync(user.Id, cancellationToken);

        return AuthCommandResult.Success(
            CreateAuthResponse(user, roles, accessToken),
            refreshToken);
    }

    public async Task<AuthCommandResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await FindByIdentifierAsync(request.Identifier);
        if (user is null)
        {
            return AuthCommandResult.Unauthorized(GenericAuthFailureMessage);
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return AuthCommandResult.Unauthorized(GenericAuthFailureMessage);
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = await refreshTokenStore.IssueAsync(user.Id, cancellationToken);

        return AuthCommandResult.Success(
            CreateAuthResponse(user, roles, accessToken),
            refreshToken);
    }

    public async Task<AuthCommandResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var rotationResult = await refreshTokenStore.RotateAsync(refreshToken, cancellationToken);
        if (!rotationResult.Succeeded || rotationResult.UserId is null || rotationResult.RefreshToken is null)
        {
            return AuthCommandResult.Unauthorized("Invalid or expired refresh token.");
        }

        var user = await userManager.FindByIdAsync(rotationResult.UserId.Value.ToString());
        if (user is null)
        {
            return AuthCommandResult.Unauthorized("Invalid or expired refresh token.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        return AuthCommandResult.Success(
            CreateAuthResponse(user, roles, accessToken),
            rotationResult.RefreshToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await refreshTokenStore.RevokeAsync(refreshToken, cancellationToken);
    }

    public async Task<MeResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new MeResponse
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        };
    }

    public async Task<ForgotPasswordCommandResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        bool includeDebugResetToken,
        CancellationToken cancellationToken)
    {
        var user = await FindByIdentifierAsync(request.Identifier);
        if (user is null)
        {
            return new ForgotPasswordCommandResult
            {
                Response = new ForgotPasswordResponse
                {
                    Message = GenericForgotPasswordMessage
                }
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        return new ForgotPasswordCommandResult
        {
            Response = new ForgotPasswordResponse
            {
                Message = GenericForgotPasswordMessage,
                DebugResetToken = includeDebugResetToken ? resetToken : null
            }
        };
    }

    public async Task<ResetPasswordCommandResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindByIdentifierAsync(request.Identifier);
        if (user is null)
        {
            return ResetPasswordCommandResult.ValidationError(GenericResetPasswordFailureMessage);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var resetResult = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return ResetPasswordCommandResult.ValidationError(
                $"{GenericResetPasswordFailureMessage} {string.Join("; ", resetResult.Errors.Select(x => x.Description))}");
        }

        return ResetPasswordCommandResult.Success();
    }

    private async Task<AuthUser?> FindByIdentifierAsync(string identifier)
    {
        var normalizedIdentifier = StorageTextNormalizer.NormalizeKey(identifier);
        if (normalizedIdentifier.Contains('@'))
        {
            var byEmail = await userManager.FindByEmailAsync(normalizedIdentifier);
            if (byEmail is not null)
            {
                return byEmail;
            }
        }

        var byUserName = await userManager.FindByNameAsync(normalizedIdentifier);
        if (byUserName is not null)
        {
            return byUserName;
        }

        return await userManager.FindByEmailAsync(normalizedIdentifier);
    }

    private async Task EnsureRolesExistAsync(CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(AuthRoles.User))
        {
            var createUserRoleResult = await roleManager.CreateAsync(new AuthRole
            {
                Name = AuthRoles.User
            });

            if (!createUserRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create '{AuthRoles.User}' role: {string.Join("; ", createUserRoleResult.Errors.Select(x => x.Description))}");
            }
        }

        if (!await roleManager.RoleExistsAsync(AuthRoles.Admin))
        {
            var createAdminRoleResult = await roleManager.CreateAsync(new AuthRole
            {
                Name = AuthRoles.Admin
            });

            if (!createAdminRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create '{AuthRoles.Admin}' role: {string.Join("; ", createAdminRoleResult.Errors.Select(x => x.Description))}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static AuthResponse CreateAuthResponse(
        AuthUser user,
        IEnumerable<string> roles,
        AccessTokenEnvelope accessToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    private static AuthCommandResult MapIdentityFailure(IEnumerable<IdentityError> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Any(x =>
                x.Code == nameof(IdentityErrorDescriber.DuplicateEmail)
                || x.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return AuthCommandResult.Conflict(string.Join("; ", errorList.Select(x => x.Description)));
        }

        return AuthCommandResult.ValidationError(string.Join("; ", errorList.Select(x => x.Description)));
    }
}
