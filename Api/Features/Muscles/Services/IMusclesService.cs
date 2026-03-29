using Api.Features.Muscles.Contracts;

namespace Api.Features.Muscles.Services;

public interface IMusclesService
{
    Task<List<MuscleResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<List<MuscleResponse>> SearchAsync(string searchTerm, CancellationToken cancellationToken);

    Task<List<MuscleResponse>> GetByGroupAsync(string groupName, CancellationToken cancellationToken);

    Task<CreateMusclesBulkResult> CreateBulkAsync(
        List<CreateMuscleRequest> requests,
        CancellationToken cancellationToken);
}

