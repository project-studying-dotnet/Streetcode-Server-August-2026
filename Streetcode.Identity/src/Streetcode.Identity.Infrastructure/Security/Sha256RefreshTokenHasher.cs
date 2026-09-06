using System.Security.Cryptography;
using System.Text;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Infrastructure.Security;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
