using Api.Features.Plans.Services;

namespace Api.Features.Plans;

public static class DependencyInjection
{
    public static IServiceCollection AddPlansFeature(this IServiceCollection services)
    {
        services.AddScoped<IPlansService, PlansService>();
        return services;
    }
}
