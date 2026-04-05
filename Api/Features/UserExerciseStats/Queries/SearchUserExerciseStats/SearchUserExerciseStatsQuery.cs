using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.UserExerciseStats.Contracts;

namespace Api.Features.UserExerciseStats.Queries.SearchUserExerciseStats;

public sealed record SearchUserExerciseStatsQuery(int UserId, SearchUserExerciseStatsRequest Request)
    : IQuery<PagedResponse<UserExerciseStatResponse>>;
