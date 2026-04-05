using Api.Features.Workouts.Services;

namespace Api.Features.Workouts;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkoutFeature(this IServiceCollection services)
    {
        services.AddScoped<IWorkoutsService, WorkoutsService>();
        return services;
    }
}
