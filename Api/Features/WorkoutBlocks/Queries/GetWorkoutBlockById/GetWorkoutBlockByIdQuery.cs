using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;

namespace Api.Features.WorkoutBlocks.Queries.GetWorkoutBlockById;

public sealed record GetWorkoutBlockByIdQuery(int UserId, int WorkoutBlockId) : IQuery<WorkoutBlockResponse?>;
