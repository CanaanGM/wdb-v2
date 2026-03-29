using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;

namespace Api.Features.Muscles.Queries.SearchMuscles;

public sealed record SearchMusclesQuery(string SearchTerm) : IQuery<List<MuscleResponse>>;

