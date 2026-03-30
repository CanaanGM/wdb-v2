using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlock;

public sealed class CreateWorkoutBlockCommandHandler(IWorkoutBlocksService workoutBlocksService)
    : ICommandHandler<CreateWorkoutBlockCommand, WorkoutBlockOperationResult<WorkoutBlockResponse>>
{
    public async Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> Handle(
        CreateWorkoutBlockCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.CreateAsync(command.UserId, command.Request, cancellationToken);
    }
}
