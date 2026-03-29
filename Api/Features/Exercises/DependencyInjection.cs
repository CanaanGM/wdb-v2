using Api.Features.Exercises.Services;

namespace Api.Features.Exercises;

public static class DependencyInjection
{
    public static IServiceCollection AddExerciseFeature(this IServiceCollection services)
    {
        services.AddScoped<IExercisesService, ExercisesService>();
        return services;
    }
}
