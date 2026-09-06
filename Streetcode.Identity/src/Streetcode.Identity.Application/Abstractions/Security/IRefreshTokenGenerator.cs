namespace Streetcode.Identity.Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
}