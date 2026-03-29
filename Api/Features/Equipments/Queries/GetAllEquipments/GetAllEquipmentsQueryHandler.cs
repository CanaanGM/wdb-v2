using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Queries.GetAllEquipments;

public sealed class GetAllEquipmentsQueryHandler(IEquipmentsService equipmentsService)
    : IQueryHandler<GetAllEquipmentsQuery, List<EquipmentResponse>>
{
    public async Task<List<EquipmentResponse>> Handle(GetAllEquipmentsQuery query, CancellationToken cancellationToken)
    {
        return await equipmentsService.GetAllAsync(cancellationToken);
    }
}
