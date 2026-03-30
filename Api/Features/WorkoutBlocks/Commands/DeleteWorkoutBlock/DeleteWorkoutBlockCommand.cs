using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.DeleteWorkoutBlock;

public sealed record DeleteWorkoutBlockCommand(int UserId, int WorkoutBlockId)
    : ICommand<WorkoutBlockOperationResult>;
