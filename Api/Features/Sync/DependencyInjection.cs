using Api.Features.Sync.Services;

namespace Api.Features.Sync;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncFeature(this IServiceCollection services)
    {
        services.AddScoped<ISyncService, SyncService>();
        return services;
    }
}
