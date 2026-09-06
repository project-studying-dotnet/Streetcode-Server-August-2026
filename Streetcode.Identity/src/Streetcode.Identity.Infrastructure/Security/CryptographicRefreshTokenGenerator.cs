using System.Security.Cryptography;
using Streetcode.Identity.Application.Abstractions.Security;

namespace Streetcode.Identity.Infrastructure.Security;

public sealed class CryptographicRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToHexString(randomBytes);
    }
}
