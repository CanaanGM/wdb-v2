using Api.Application.Text;
using Api.Features.Auth.Options;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Api.Features.Auth.Bootstrap;

public sealed class AuthBootstrapHostedService(
    IServiceProvider serviceProvider,
    IOptions<BootstrapAdminOptions> bootstrapAdminOptions,
    ILogger<AuthBootstrapHostedService> logger) : IHostedService
{
    private readonly BootstrapAdminOptions _bootstrapAdminOptions = bootstrapAdminOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AuthRole>>();

        await EnsureRoleAsync(roleManager, AuthRoles.User);
        await EnsureRoleAsync(roleManager, AuthRoles.Admin);

        if (!_bootstrapAdminOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email)
            || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Username)
            || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password))
        {
            logger.LogWarning("Bootstrap admin is enabled but email/username/password is missing. Skipping bootstrap.");
            return;
        }

        var normalizedEmail = StorageTextNormalizer.NormalizeKey(_bootstrapAdminOptions.Email);
        var normalizedUsername = StorageTextNormalizer.NormalizeKey(_bootstrapAdminOptions.Username);

        var user = await userManager.FindByEmailAsync(normalizedEmail)
            ?? await userManager.FindByNameAsync(normalizedUsername);

        if (user is null)
        {
            user = new AuthUser
            {
                Email = normalizedEmail,
                UserName = normalizedUsername,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, _bootstrapAdminOptions.Password);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create bootstrap admin: {Errors}",
                    string.Join("; ", createResult.Errors.Select(x => x.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, AuthRoles.User))
        {
            var addUserRoleResult = await userManager.AddToRoleAsync(user, AuthRoles.User);
            if (!addUserRoleResult.Succeeded)
            {
                logger.LogError(
                    "Failed to add bootstrap admin user role: {Errors}",
                    string.Join("; ", addUserRoleResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, AuthRoles.Admin))
        {
            var addAdminRoleResult = await userManager.AddToRoleAsync(user, AuthRoles.Admin);
            if (!addAdminRoleResult.Succeeded)
            {
                logger.LogError(
                    "Failed to add bootstrap admin role: {Errors}",
                    string.Join("; ", addAdminRoleResult.Errors.Select(x => x.Description)));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureRoleAsync(RoleManager<AuthRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        await roleManager.CreateAsync(new AuthRole
        {
            Name = roleName
        });
    }
}
