using Api.Application.Cqrs;
using Api.Features.UserExerciseStats.Contracts;
using Api.Features.UserExerciseStats.Services;

namespace Api.Features.UserExerciseStats.Queries.GetUserExerciseStatByExerciseId;

public sealed class GetUserExerciseStatByExerciseIdQueryHandler(IUserExerciseStatsService userExerciseStatsService)
    : IQueryHandler<GetUserExerciseStatByExerciseIdQuery, UserExerciseStatResponse?>
{
    public async Task<UserExerciseStatResponse?> Handle(
        GetUserExerciseStatByExerciseIdQuery query,
        CancellationToken cancellationToken)
    {
        return await userExerciseStatsService.GetByExerciseIdAsync(query.UserId, query.ExerciseId, cancellationToken);
    }
}
