using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.DeleteWorkoutBlock;

public sealed class DeleteWorkoutBlockCommandHandler(IWorkoutBlocksService workoutBlocksService)
    : ICommandHandler<DeleteWorkoutBlockCommand, WorkoutBlockOperationResult>
{
    public async Task<WorkoutBlockOperationResult> Handle(
        DeleteWorkoutBlockCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.DeleteAsync(command.UserId, command.WorkoutBlockId, cancellationToken);
    }
}
