using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.Exercises.Contracts;

namespace Api.Features.Exercises.Queries.GetAllExercises;

public sealed record GetAllExercisesQuery(GetExercisesRequest Request) : IQuery<PagedResponse<ExerciseResponse>>;
