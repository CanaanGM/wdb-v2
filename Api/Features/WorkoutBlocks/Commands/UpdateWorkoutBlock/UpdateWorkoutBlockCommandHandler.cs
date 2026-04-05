using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.UpdateWorkoutBlock;

public sealed class UpdateWorkoutBlockCommandHandler(IWorkoutBlocksService workoutBlocksService)
    : ICommandHandler<UpdateWorkoutBlockCommand, WorkoutBlockOperationResult<WorkoutBlockResponse>>
{
    public async Task<WorkoutBlockOperationResult<WorkoutBlockResponse>> Handle(
        UpdateWorkoutBlockCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.UpdateAsync(command.UserId, command.WorkoutBlockId, command.Request, cancellationToken);
    }
}
