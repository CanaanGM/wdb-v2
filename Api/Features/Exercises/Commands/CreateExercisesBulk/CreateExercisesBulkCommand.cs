using Api.Application.Cqrs;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Commands.CreateExercisesBulk;

public sealed record CreateExercisesBulkCommand(List<CreateExerciseRequest> Requests)
    : ICommand<CreateExercisesBulkResult>;

