using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Queries.GetWorkoutById;

public sealed class GetWorkoutByIdQueryHandler(IWorkoutsService workoutsService)
    : IQueryHandler<GetWorkoutByIdQuery, WorkoutResponse?>
{
    public async Task<WorkoutResponse?> Handle(GetWorkoutByIdQuery query, CancellationToken cancellationToken)
    {
        return await workoutsService.GetByIdAsync(query.UserId, query.WorkoutId, cancellationToken);
    }
}
