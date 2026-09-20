using Microsoft.EntityFrameworkCore.Storage;

namespace Streetcode.WebApi.Identity;

public interface IRegistrationTransactionFactory
{
    Task<IDbContextTransaction> BeginAsync();
}
