using Api.Application.Cqrs;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.CreateEquipmentsBulk;

public sealed class CreateEquipmentsBulkCommandHandler(IEquipmentsService equipmentsService)
    : ICommandHandler<CreateEquipmentsBulkCommand, CreateEquipmentsBulkResult>
{
    public async Task<CreateEquipmentsBulkResult> Handle(
        CreateEquipmentsBulkCommand command,
        CancellationToken cancellationToken)
    {
        return await equipmentsService.CreateBulkAsync(command.Requests, cancellationToken);
    }
}
