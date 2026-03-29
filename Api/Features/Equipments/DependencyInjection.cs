using Api.Features.Equipments.Services;

namespace Api.Features.Equipments;

public static class DependencyInjection
{
    public static IServiceCollection AddEquipmentFeature(this IServiceCollection services)
    {
        services.AddScoped<IEquipmentsService, EquipmentsService>();
        return services;
    }
}
