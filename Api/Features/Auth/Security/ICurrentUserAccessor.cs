namespace Api.Features.Auth.Security;

public interface ICurrentUserAccessor
{
    int? GetUserId();
}
