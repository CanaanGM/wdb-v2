using Api.Application.Cqrs;
using Api.Application.Contracts.Querying;
using Api.Features.UserExerciseStats.Contracts;
using Api.Features.UserExerciseStats.Services;

namespace Api.Features.UserExerciseStats.Queries.SearchUserExerciseStats;

public sealed class SearchUserExerciseStatsQueryHandler(IUserExerciseStatsService userExerciseStatsService)
    : IQueryHandler<SearchUserExerciseStatsQuery, PagedResponse<UserExerciseStatResponse>>
{
    public async Task<PagedResponse<UserExerciseStatResponse>> Handle(
        SearchUserExerciseStatsQuery query,
        CancellationToken cancellationToken)
    {
        return await userExerciseStatsService.SearchAsync(query.UserId, query.Request, cancellationToken);
    }
}
