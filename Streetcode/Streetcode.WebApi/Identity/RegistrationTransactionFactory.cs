using Microsoft.EntityFrameworkCore.Storage;

namespace Streetcode.WebApi.Identity;

public sealed class RegistrationTransactionFactory(RegistrationDbContext context)
    : IRegistrationTransactionFactory
{
    public Task<IDbContextTransaction> BeginAsync() =>
        context.Database.BeginTransactionAsync();
}
