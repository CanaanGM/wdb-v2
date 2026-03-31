using Api.Features.UserExerciseStats.Options;
using Api.Features.UserExerciseStats.Services;
using Microsoft.Extensions.Configuration;

namespace Api.Features.UserExerciseStats;

public static class DependencyInjection
{
    public static IServiceCollection AddUserExerciseStatsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UserExerciseStatsMaintenanceOptions>(
            configuration.GetSection(UserExerciseStatsMaintenanceOptions.SectionName));
        services.AddScoped<IUserExerciseStatsService, UserExerciseStatsService>();
        services.AddHostedService<UserExerciseStatsMaintenanceHostedService>();
        return services;
    }
}
