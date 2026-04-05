using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Queries.SearchWorkoutBlocks;

public sealed class SearchWorkoutBlocksQueryHandler(IWorkoutBlocksService workoutBlocksService)
    : IQueryHandler<SearchWorkoutBlocksQuery, PagedResponse<WorkoutBlockResponse>>
{
    public async Task<PagedResponse<WorkoutBlockResponse>> Handle(
        SearchWorkoutBlocksQuery query,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.SearchAsync(query.UserId, query.Request, cancellationToken);
    }
}
