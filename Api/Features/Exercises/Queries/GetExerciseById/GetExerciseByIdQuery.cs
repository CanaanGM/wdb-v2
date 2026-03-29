using Api.Application.Cqrs;
using Api.Features.Exercises.Contracts;

namespace Api.Features.Exercises.Queries.GetExerciseById;

public sealed record GetExerciseByIdQuery(int Id) : IQuery<ExerciseResponse?>;

