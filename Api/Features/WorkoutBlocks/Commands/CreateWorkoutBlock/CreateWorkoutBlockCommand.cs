using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlock;

public sealed record CreateWorkoutBlockCommand(int UserId, CreateWorkoutBlockRequest Request)
    : ICommand<WorkoutBlockOperationResult<WorkoutBlockResponse>>;
