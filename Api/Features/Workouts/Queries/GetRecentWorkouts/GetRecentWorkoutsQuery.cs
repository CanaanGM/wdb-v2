using Api.Application.Cqrs;
using Api.Features.Workouts.Contracts;

namespace Api.Features.Workouts.Queries.GetRecentWorkouts;

public sealed record GetRecentWorkoutsQuery(int UserId, int Hours) : IQuery<List<WorkoutResponse>>;
