using Api.Application.Cqrs;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.DeleteWorkout;

public sealed class DeleteWorkoutCommandHandler(IWorkoutsService workoutsService)
    : ICommandHandler<DeleteWorkoutCommand, WorkoutOperationResult>
{
    public async Task<WorkoutOperationResult> Handle(DeleteWorkoutCommand command, CancellationToken cancellationToken)
    {
        return await workoutsService.DeleteAsync(command.UserId, command.WorkoutId, cancellationToken);
    }
}
