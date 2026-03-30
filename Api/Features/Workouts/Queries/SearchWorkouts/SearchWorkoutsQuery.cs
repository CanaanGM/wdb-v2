using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.Workouts.Contracts;

namespace Api.Features.Workouts.Queries.SearchWorkouts;

public sealed record SearchWorkoutsQuery(int UserId, SearchWorkoutsRequest Request)
    : IQuery<PagedResponse<WorkoutResponse>>;
