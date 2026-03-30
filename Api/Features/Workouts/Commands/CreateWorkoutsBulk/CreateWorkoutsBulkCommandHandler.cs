using Api.Application.Cqrs;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.CreateWorkoutsBulk;

public sealed class CreateWorkoutsBulkCommandHandler(IWorkoutsService workoutsService)
    : ICommandHandler<CreateWorkoutsBulkCommand, WorkoutOperationResult<int>>
{
    public async Task<WorkoutOperationResult<int>> Handle(
        CreateWorkoutsBulkCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutsService.CreateBulkAsync(command.UserId, command.Requests, cancellationToken);
    }
}
