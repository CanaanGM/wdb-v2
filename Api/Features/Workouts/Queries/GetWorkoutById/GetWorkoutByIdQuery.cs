using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;

namespace Api.Features.Workouts.Queries.GetWorkoutById;

public sealed record GetWorkoutByIdQuery(int UserId, int WorkoutId) : IQuery<WorkoutResponse?>;
