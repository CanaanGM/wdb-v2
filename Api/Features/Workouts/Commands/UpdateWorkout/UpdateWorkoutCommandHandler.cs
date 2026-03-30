using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.UpdateWorkout;

public sealed class UpdateWorkoutCommandHandler(IWorkoutsService workoutsService)
    : ICommandHandler<UpdateWorkoutCommand, WorkoutOperationResult<WorkoutResponse>>
{
    public async Task<WorkoutOperationResult<WorkoutResponse>> Handle(
        UpdateWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutsService.UpdateAsync(command.UserId, command.WorkoutId, command.Request, cancellationToken);
    }
}
