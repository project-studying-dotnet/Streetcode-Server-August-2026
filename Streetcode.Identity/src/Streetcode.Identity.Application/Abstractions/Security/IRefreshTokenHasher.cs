namespace Streetcode.Identity.Application.Abstractions.Security;

public interface IRefreshTokenHasher
{
    string ComputeHash(string token);
}