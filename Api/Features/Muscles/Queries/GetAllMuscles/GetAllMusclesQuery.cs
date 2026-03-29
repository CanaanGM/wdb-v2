using Api.Application.Cqrs;
using Api.Features.Muscles.Contracts;

namespace Api.Features.Muscles.Queries.GetAllMuscles;

public sealed record GetAllMusclesQuery : IQuery<List<MuscleResponse>>;

