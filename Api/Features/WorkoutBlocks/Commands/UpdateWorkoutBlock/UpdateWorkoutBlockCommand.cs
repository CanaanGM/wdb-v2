using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.UpdateWorkoutBlock;

public sealed record UpdateWorkoutBlockCommand(int UserId, int WorkoutBlockId, UpdateWorkoutBlockRequest Request)
    : ICommand<WorkoutBlockOperationResult<WorkoutBlockResponse>>;
