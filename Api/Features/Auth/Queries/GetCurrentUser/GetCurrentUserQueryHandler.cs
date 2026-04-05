using Api.Application.Cqrs;
using Api.Features.Auth.Contracts;
using Api.Features.Auth.Services;

namespace Api.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IAuthService authService)
    : IQueryHandler<GetCurrentUserQuery, MeResponse?>
{
    public async Task<MeResponse?> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        return await authService.GetCurrentUserAsync(query.UserId, cancellationToken);
    }
}
