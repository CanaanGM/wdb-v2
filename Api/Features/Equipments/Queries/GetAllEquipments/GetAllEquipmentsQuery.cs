using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;

namespace Api.Features.Equipments.Queries.GetAllEquipments;

public sealed record GetAllEquipmentsQuery : IQuery<List<EquipmentResponse>>;
