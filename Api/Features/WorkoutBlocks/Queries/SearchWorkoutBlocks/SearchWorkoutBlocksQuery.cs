using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.WorkoutBlocks.Contracts;

namespace Api.Features.WorkoutBlocks.Queries.SearchWorkoutBlocks;

public sealed record SearchWorkoutBlocksQuery(int UserId, SearchWorkoutBlocksRequest Request)
    : IQuery<PagedResponse<WorkoutBlockResponse>>;
