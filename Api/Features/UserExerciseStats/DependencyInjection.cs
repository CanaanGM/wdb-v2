using Api.Features.UserExerciseStats.Options;
using Api.Features.UserExerciseStats.Services;
using Microsoft.Extensions.Configuration;

namespace Api.Features.UserExerciseStats;

public static class DependencyInjection
{
    private const string RecomputeAllOnStartupKey =
        $"{UserExerciseStatsMaintenanceOptions.SectionName}:{nameof(UserExerciseStatsMaintenanceOptions.RecomputeAllOnStartup)}";

    public static IServiceCollection AddUserExerciseStatsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<UserExerciseStatsMaintenanceOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var rawValue = GetNonEmptyConfigurationValue(config, RecomputeAllOnStartupKey);
                if (rawValue is null)
                {
                    options.RecomputeAllOnStartup = false;
                    return;
                }

                if (!bool.TryParse(rawValue, out var recomputeAllOnStartup))
                {
                    throw new InvalidOperationException(
                        $"Failed to convert configuration value '{rawValue}' at '{RecomputeAllOnStartupKey}' to type '{typeof(bool)}'.");
                }

                options.RecomputeAllOnStartup = recomputeAllOnStartup;
            });

        services.AddScoped<IUserExerciseStatsService, UserExerciseStatsService>();
        services.AddHostedService<UserExerciseStatsMaintenanceHostedService>();
        return services;
    }

    private static string? GetNonEmptyConfigurationValue(IConfiguration configuration, string key)
    {
        if (configuration is IConfigurationRoot root)
        {
            foreach (var provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out var candidate) && !string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
