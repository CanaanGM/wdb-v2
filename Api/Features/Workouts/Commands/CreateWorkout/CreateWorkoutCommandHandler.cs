using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.CreateWorkout;

public sealed class CreateWorkoutCommandHandler(IWorkoutsService workoutsService)
    : ICommandHandler<CreateWorkoutCommand, WorkoutOperationResult<WorkoutResponse>>
{
    public async Task<WorkoutOperationResult<WorkoutResponse>> Handle(
        CreateWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        return await workoutsService.CreateAsync(command.UserId, command.Request, cancellationToken);
    }
}
