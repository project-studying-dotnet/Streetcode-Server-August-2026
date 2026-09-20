using Microsoft.EntityFrameworkCore;
using Streetcode.Email.Infrastructure.Persistence;

namespace Streetcode.Email.WebApi.Extensions;

internal static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        await using var scope = application.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EmailDbContext>();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseMigrationExtensions));

        logger.LogInformation(
            "Applying Email database migrations.");

        await dbContext.Database.MigrateAsync();

        logger.LogInformation(
            "Email database migrations were applied successfully.");
    }
}
