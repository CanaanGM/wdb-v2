using Api.Features.TrainingTypes.Services;

namespace Api.Features.TrainingTypes;

public static class DependencyInjection
{
    public static IServiceCollection AddTrainingTypesFeature(this IServiceCollection services)
    {
        services.AddScoped<ITrainingTypesService, TrainingTypesService>();
        return services;
    }
}
