using Api.Application.Cqrs;
using Api.Features.UserExerciseStats.Contracts;

namespace Api.Features.UserExerciseStats.Queries.GetUserExerciseStatByExerciseId;

public sealed record GetUserExerciseStatByExerciseIdQuery(int UserId, int ExerciseId) : IQuery<UserExerciseStatResponse?>;
