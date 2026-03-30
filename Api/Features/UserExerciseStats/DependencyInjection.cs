using Api.Features.UserExerciseStats.Services;

namespace Api.Features.UserExerciseStats;

public static class DependencyInjection
{
    public static IServiceCollection AddUserExerciseStatsFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserExerciseStatsService, UserExerciseStatsService>();
        return services;
    }
}
