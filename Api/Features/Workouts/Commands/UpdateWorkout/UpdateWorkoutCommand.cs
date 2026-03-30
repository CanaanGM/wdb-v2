using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.UpdateWorkout;

public sealed record UpdateWorkoutCommand(int UserId, int WorkoutId, UpdateWorkoutRequest Request)
    : ICommand<WorkoutOperationResult<WorkoutResponse>>;
