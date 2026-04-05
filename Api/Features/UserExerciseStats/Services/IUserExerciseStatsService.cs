using Api.Application.Contracts.Querying;
using Api.Features.UserExerciseStats.Contracts;

namespace Api.Features.UserExerciseStats.Services;

public interface IUserExerciseStatsService
{
    Task<PagedResponse<UserExerciseStatResponse>> SearchAsync(int userId, SearchUserExerciseStatsRequest request, CancellationToken cancellationToken);

    Task<UserExerciseStatResponse?> GetByExerciseIdAsync(int userId, int exerciseId, CancellationToken cancellationToken);

    Task RecomputeForExercisesAsync(int userId, IReadOnlyCollection<int> exerciseIds, CancellationToken cancellationToken);

    Task RecomputeAllAsync(CancellationToken cancellationToken);
}
