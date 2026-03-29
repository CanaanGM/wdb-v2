using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Queries.SearchEquipments;

public sealed class SearchEquipmentsQueryHandler(IEquipmentsService equipmentsService)
    : IQueryHandler<SearchEquipmentsQuery, List<EquipmentResponse>>
{
    public async Task<List<EquipmentResponse>> Handle(
        SearchEquipmentsQuery query,
        CancellationToken cancellationToken)
    {
        return await equipmentsService.SearchAsync(query.SearchTerm, cancellationToken);
    }
}
