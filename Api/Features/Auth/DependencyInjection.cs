using System.Text;
using System.Security.Claims;
using Api.Features.Auth.Bootstrap;
using Api.Features.Auth.Options;
using Api.Features.Auth.Security;
using Api.Features.Auth.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Features.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var refreshTokenOptions = configuration.GetSection(RefreshTokenOptions.SectionName).Get<RefreshTokenOptions>() ?? new RefreshTokenOptions();
        var bootstrapAdminOptions = configuration.GetSection(BootstrapAdminOptions.SectionName).Get<BootstrapAdminOptions>() ?? new BootstrapAdminOptions();

        ValidateJwtOptions(jwtOptions);
        ValidateRefreshTokenOptions(refreshTokenOptions);
        ValidateBootstrapOptions(bootstrapAdminOptions, environment);

        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenSecurityStamp = context.Principal?.FindFirstValue(AuthClaimTypes.SecurityStamp);

                        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AuthUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());
                        if (user is null
                            || string.IsNullOrWhiteSpace(user.SecurityStamp)
                            || !string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Token is no longer valid.");
                        }
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services
            .AddIdentityCore<AuthUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AuthRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<WorkoutLogDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddHostedService<AuthBootstrapHostedService>();

        return services;
    }

    private static void ValidateJwtOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("Auth:Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Auth:Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            throw new InvalidOperationException("Auth:Jwt:Secret is required.");
        }

        if (options.Secret.Length < 32)
        {
            throw new InvalidOperationException("Auth:Jwt:Secret must be at least 32 characters long.");
        }

        if (string.Equals(options.Secret, JwtOptions.DefaultSecret, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Auth:Jwt:Secret is using the default placeholder value. Configure a strong secret before startup.");
        }

        if (options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("Auth:Jwt:AccessTokenMinutes must be greater than 0.");
        }
    }

    private static void ValidateRefreshTokenOptions(RefreshTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CookieName))
        {
            throw new InvalidOperationException("Auth:RefreshToken:CookieName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.CookiePath) || !options.CookiePath.StartsWith('/'))
        {
            throw new InvalidOperationException("Auth:RefreshToken:CookiePath must be an absolute path starting with '/'.");
        }

        if (options.RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("Auth:RefreshToken:RefreshTokenDays must be greater than 0.");
        }
    }

    private static void ValidateBootstrapOptions(BootstrapAdminOptions options, IHostEnvironment environment)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email)
            || string.IsNullOrWhiteSpace(options.Username)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "Auth:BootstrapAdmin is enabled, but Email/Username/Password is missing.");
        }

        if (options.Password.Length < 12)
        {
            throw new InvalidOperationException(
                "Auth:BootstrapAdmin:Password must be at least 12 characters when bootstrap is enabled.");
        }

        if (string.Equals(options.Password, BootstrapAdminOptions.DefaultPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Auth:BootstrapAdmin:Password is using the default placeholder value. Set a strong custom password.");
        }
    }
}
