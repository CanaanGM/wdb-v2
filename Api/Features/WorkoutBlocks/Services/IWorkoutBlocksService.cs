using Api.Application.Contracts.Querying;
using Api.Features.WorkoutBlocks.Contracts;

namespace Api.Features.WorkoutBlocks.Services;

public interface IWorkoutBlocksService
{
    Task<PagedResponse<WorkoutBlockResponse>> SearchAsync(int userId, SearchWorkoutBlocksRequest request, CancellationToken cancellationToken);

    Task<WorkoutBlockResponse?> GetByIdAsync(int userId, int workoutBlockId, CancellationToken cancellationToken);

    Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> CreateAsync(int userId, CreateWorkoutBlockRequest request, CancellationToken cancellationToken);

    Task<WorkoutBlockOperationResult<int>> CreateBulkAsync(int userId, List<CreateWorkoutBlockRequest> requests, CancellationToken cancellationToken);

    Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> UpdateAsync(int userId, int workoutBlockId, UpdateWorkoutBlockRequest request, CancellationToken cancellationToken);

    Task<WorkoutBlockOperationResult> DeleteAsync(int userId, int workoutBlockId, CancellationToken cancellationToken);
}
