using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;

namespace Api.Features.Muscles.Queries.GetMusclesByGroup;

public sealed record GetMusclesByGroupQuery(string GroupName) : IQuery<List<MuscleResponse>>;

