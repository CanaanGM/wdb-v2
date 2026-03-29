using Api.Application.Cqrs;
using Api.Features.Equipments.Contracts;

namespace Api.Features.Equipments.Queries.SearchEquipments;

public sealed record SearchEquipmentsQuery(string SearchTerm) : IQuery<List<EquipmentResponse>>;
