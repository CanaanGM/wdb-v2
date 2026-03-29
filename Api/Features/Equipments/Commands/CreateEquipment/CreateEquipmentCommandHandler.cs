using Api.Application.Cqrs;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.CreateEquipment;

public sealed class CreateEquipmentCommandHandler(IEquipmentsService equipmentsService)
    : ICommandHandler<CreateEquipmentCommand, CreateEquipmentResult>
{
    public async Task<CreateEquipmentResult> Handle(CreateEquipmentCommand command, CancellationToken cancellationToken)
    {
        return await equipmentsService.CreateAsync(command.Request, cancellationToken);
    }
}
