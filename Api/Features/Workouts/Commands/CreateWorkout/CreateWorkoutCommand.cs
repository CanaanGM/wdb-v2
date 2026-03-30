using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.CreateWorkout;

public sealed record CreateWorkoutCommand(int UserId, CreateWorkoutRequest Request)
    : ICommand<WorkoutOperationResult<WorkoutResponse>>;
