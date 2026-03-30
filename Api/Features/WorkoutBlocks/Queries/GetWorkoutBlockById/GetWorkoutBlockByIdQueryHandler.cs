using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Queries.GetWorkoutBlockById;

public sealed class GetWorkoutBlockByIdQueryHandler(IWorkoutBlocksService workoutBlocksService)
    : IQueryHandler<GetWorkoutBlockByIdQuery, WorkoutBlockResponse?>
{
    public async Task<WorkoutBlockResponse?> Handle(
        GetWorkoutBlockByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.GetByIdAsync(query.UserId, query.WorkoutBlockId, cancellationToken);
    }
}
