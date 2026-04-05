using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;

namespace Api.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(int UserId) : IQuery<MeResponse?>;
