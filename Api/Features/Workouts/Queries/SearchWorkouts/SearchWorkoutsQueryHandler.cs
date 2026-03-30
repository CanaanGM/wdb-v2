using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Queries.SearchWorkouts;

public sealed class SearchWorkoutsQueryHandler(IWorkoutsService workoutsService)
    : IQueryHandler<SearchWorkoutsQuery, PagedResponse<WorkoutResponse>>
{
    public async Task<PagedResponse<WorkoutResponse>> Handle(SearchWorkoutsQuery query, CancellationToken cancellationToken)
    {
        return await workoutsService.SearchAsync(query.UserId, query.Request, cancellationToken);
    }
}
