using Api.Application.Cqrs;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.DeleteWorkout;

public sealed record DeleteWorkoutCommand(int UserId, int WorkoutId)
    : ICommand<WorkoutOperationResult>;
