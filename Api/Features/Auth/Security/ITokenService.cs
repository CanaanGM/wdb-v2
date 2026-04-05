using Infrastructure.Persistence.Features.Auth.Entities;

namespace Api.Features.Auth.Security;

public interface ITokenService
{
    AccessTokenEnvelope CreateAccessToken(AuthUser user, IEnumerable<string> roles);
}
