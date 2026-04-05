using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Queries.GetRecentWorkouts;

public sealed class GetRecentWorkoutsQueryHandler(IWorkoutsService workoutsService)
    : IQueryHandler<GetRecentWorkoutsQuery, List<WorkoutResponse>>
{
    public async Task<List<WorkoutResponse>> Handle(GetRecentWorkoutsQuery query, CancellationToken cancellationToken)
    {
        return await workoutsService.GetRecentAsync(query.UserId, query.Hours, cancellationToken);
    }
}
