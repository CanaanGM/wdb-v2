using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;
using Api.Features.Equipments.Services;

namespace Api.Features.Equipments.Commands.CreateEquipmentsBulk;

public sealed record CreateEquipmentsBulkCommand(List<CreateEquipmentRequest> Requests)
    : ICommand<CreateEquipmentsBulkResult>;
