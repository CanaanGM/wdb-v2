using Api.Features.Sync.Contracts;

namespace Api.Features.Sync.Services;

public interface ISyncService
{
    Task<SyncBootstrapResponse> BootstrapAsync(int userId, CancellationToken cancellationToken);

    Task<SyncPullResponse> PullAsync(int userId, long cursor, int limit, CancellationToken cancellationToken);

    Task<SyncPushResponse> PushAsync(int userId, SyncPushRequest request, CancellationToken cancellationToken);
}
