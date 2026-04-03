using Api.Application.Cqrs;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.DeleteEquipment;

public sealed class DeleteEquipmentCommandHandler(IEquipmentsService equipmentsService)
    : ICommandHandler<DeleteEquipmentCommand, DeleteEquipmentResult>
{
    public async Task<DeleteEquipmentResult> Handle(DeleteEquipmentCommand command, CancellationToken cancellationToken)
    {
        return await equipmentsService.DeleteByNameAsync(command.Name, cancellationToken);
    }
}
