using Api.Application.Cqrs;
using Api.Features.WorkoutBlocks.Contracts;
using Api.Features.WorkoutBlocks.Services;

namespace Api.Features.WorkoutBlocks.Commands.CreateWorkoutBlocksBulk;

public sealed record CreateWorkoutBlocksBulkCommand(int UserId, List<CreateWorkoutBlockRequest> Requests)
    : ICommand<WorkoutBlockOperationResult<int>>;
