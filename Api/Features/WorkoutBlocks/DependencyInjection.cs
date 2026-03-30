using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkoutBlockFeature(this IServiceCollection services)
    {
        services.AddScoped<IWorkoutBlocksService, WorkoutBlocksService>();
        return services;
    }
}
