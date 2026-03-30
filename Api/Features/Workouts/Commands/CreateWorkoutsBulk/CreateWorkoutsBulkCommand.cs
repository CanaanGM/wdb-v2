using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;
using Api.Features.Workouts.Services;

namespace Api.Features.Workouts.Commands.CreateWorkoutsBulk;

public sealed record CreateWorkoutsBulkCommand(int UserId, List<CreateWorkoutRequest> Requests)
    : ICommand<WorkoutOperationResult<int>>;
