using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;

namespace Api.Features.Muscles.Queries.SearchMuscles;

public sealed class SearchMusclesQueryHandler(IMusclesService musclesService)
    : IQueryHandler<SearchMusclesQuery, List<MuscleResponse>>
{
    public async Task<List<MuscleResponse>> Handle(SearchMusclesQuery query, CancellationToken cancellationToken)
    {
        return await musclesService.SearchAsync(query.SearchTerm, cancellationToken);
    }
}

