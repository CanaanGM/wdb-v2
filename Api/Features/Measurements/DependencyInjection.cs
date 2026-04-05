using Api.Features.Measurements.Services;

namespace Api.Features.Measurements;

public static class DependencyInjection
{
    public static IServiceCollection AddMeasurementsFeature(this IServiceCollection services)
    {
        services.AddScoped<IMeasurementsService, MeasurementsService>();
        return services;
    }
}
