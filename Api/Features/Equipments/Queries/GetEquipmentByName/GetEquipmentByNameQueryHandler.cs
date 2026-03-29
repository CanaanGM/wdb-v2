using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Queries.GetEquipmentByName;

public sealed class GetEquipmentByNameQueryHandler(IEquipmentsService equipmentsService)
    : IQueryHandler<GetEquipmentByNameQuery, EquipmentResponse?>
{
    public async Task<EquipmentResponse?> Handle(
        GetEquipmentByNameQuery query,
        CancellationToken cancellationToken)
    {
        return await equipmentsService.GetByNameAsync(query.Name, cancellationToken);
    }
}
