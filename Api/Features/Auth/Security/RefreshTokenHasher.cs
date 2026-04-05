using System.Security.Cryptography;
using System.Text;

namespace Api.Features.Auth.Security;

public static class RefreshTokenHasher
{
    public static string Hash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}
