using Api.Application.Contracts.Querying;
using Api.Features.Workouts.Contracts;

namespace Api.Features.Workouts.Services;

public interface IWorkoutsService
{
    Task<PagedResponse<WorkoutResponse>> SearchAsync(int userId, SearchWorkoutsRequest request, CancellationToken cancellationToken);

    Task<WorkoutResponse?> GetByIdAsync(int userId, int workoutId, CancellationToken cancellationToken);

    Task<List<WorkoutResponse>> GetRecentAsync(int userId, int hours, CancellationToken cancellationToken);

    Task<WorkoutOperationResult<WorkoutResponse>> CreateAsync(int userId, CreateWorkoutRequest request, CancellationToken cancellationToken);

    Task<WorkoutOperationResult<int>> CreateBulkAsync(int userId, List<CreateWorkoutRequest> requests, CancellationToken cancellationToken);

    Task<WorkoutOperationResult<WorkoutResponse>> UpdateAsync(int userId, int workoutId, UpdateWorkoutRequest request, CancellationToken cancellationToken);

    Task<WorkoutOperationResult> DeleteAsync(int userId, int workoutId, CancellationToken cancellationToken);
}
