using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;

namespace Api.Features.Equipments.Queries.GetEquipmentByName;

public sealed record GetEquipmentByNameQuery(string Name) : IQuery<EquipmentResponse?>;
