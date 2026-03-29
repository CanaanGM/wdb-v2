using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;

namespace Api.Features.Muscles.Queries.GetMusclesByGroup;

public sealed class GetMusclesByGroupQueryHandler(IMusclesService musclesService)
    : IQueryHandler<GetMusclesByGroupQuery, List<MuscleResponse>>
{
    public async Task<List<MuscleResponse>> Handle(
        GetMusclesByGroupQuery query,
        CancellationToken cancellationToken)
    {
        return await musclesService.GetByGroupAsync(query.GroupName, cancellationToken);
    }
}

