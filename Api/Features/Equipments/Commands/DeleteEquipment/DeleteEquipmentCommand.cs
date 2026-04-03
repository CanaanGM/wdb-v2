using Api.Application.Cqrs;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.DeleteEquipment;

public sealed record DeleteEquipmentCommand(string Name) : ICommand<DeleteEquipmentResult>;
