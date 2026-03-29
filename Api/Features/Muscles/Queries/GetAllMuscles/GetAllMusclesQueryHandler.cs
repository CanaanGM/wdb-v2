using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;
using Api.Features.Muscles.Services;

namespace Api.Features.Muscles.Queries.GetAllMuscles;

public sealed class GetAllMusclesQueryHandler(IMusclesService musclesService)
    : IQueryHandler<GetAllMusclesQuery, List<MuscleResponse>>
{
    public async Task<List<MuscleResponse>> Handle(GetAllMusclesQuery query, CancellationToken cancellationToken)
    {
        return await musclesService.GetAllAsync(cancellationToken);
    }
}

