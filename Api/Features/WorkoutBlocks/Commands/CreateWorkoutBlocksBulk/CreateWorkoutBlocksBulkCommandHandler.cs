using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlocksBulk;

public sealed class CreateWorkoutBlocksBulkCommandHandler(IWorkoutBlocksService workoutBlocksService)
    : ICommandHandler<CreateWorkoutBlocksBulkCommand, WorkoutBlockOperationResult<int>>
{
    public async Task<WorkoutBlockOperationResult<int>> Handle(
        CreateWorkoutBlocksBulkCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutBlocksService.CreateBulkAsync(command.UserId, command.Requests, cancellationToken);
    }
}
