using Api.Features.TrainingTypes.Contracts;

namespace Api.Features.TrainingTypes.Services;

public interface ITrainingTypesService
{
    Task<List<TrainingTypeResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<TrainingTypeOperationResult<TrainingTypeResponse>> CreateAsync(
        CreateTrainingTypeRequest request,
        CancellationToken cancellationToken);

    Task<TrainingTypeOperationResult<int>> CreateBulkAsync(
        IReadOnlyList<CreateTrainingTypeRequest> requests,
        CancellationToken cancellationToken);

    Task<TrainingTypeOperationResult<TrainingTypeResponse>> UpdateAsync(
        int id,
        UpdateTrainingTypeRequest request,
        CancellationToken cancellationToken);

    Task<TrainingTypeOperationResult> DeleteAsync(int id, CancellationToken cancellationToken);
}
