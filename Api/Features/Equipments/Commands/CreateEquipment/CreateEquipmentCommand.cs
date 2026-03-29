using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.CreateEquipment;

public sealed record CreateEquipmentCommand(CreateEquipmentRequest Request) : ICommand<CreateEquipmentResult>;
