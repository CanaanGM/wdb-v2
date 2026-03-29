using Api.Application.Cqrs;
using Api.Features.Exercises.Contracts;
using Api.Features.Exercises.Services;

namespace Api.Features.Exercises.Commands.CreateExercise;

public sealed record CreateExerciseCommand(CreateExerciseRequest Request) : ICommand<CreateExerciseResult>;

