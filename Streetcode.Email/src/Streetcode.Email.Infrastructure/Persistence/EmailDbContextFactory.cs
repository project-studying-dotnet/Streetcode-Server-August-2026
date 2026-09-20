using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Streetcode.Email.Infrastructure.Persistence;

public sealed class EmailDbContextFactory
    : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args)
    {
        const string connectionStringVariable =
            "ConnectionStrings__EmailDatabase";

        var connectionString = Environment.GetEnvironmentVariable(
            connectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable '{connectionStringVariable}' " +
                "is not configured.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<EmailDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        return new EmailDbContext(optionsBuilder.Options);
    }
}
