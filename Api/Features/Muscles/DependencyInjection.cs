using Api.Features.Muscles.Services;

namespace Api.Features.Muscles;

public static class DependencyInjection
{
    public static IServiceCollection AddMuscleFeature(this IServiceCollection services)
    {
        services.AddScoped<IMusclesService, MusclesService>();
        return services;
    }
}
