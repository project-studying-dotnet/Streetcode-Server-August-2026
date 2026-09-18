using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Streetcode.WebApi.Identity;

public class RegistrationDbContextFactory : IDesignTimeDbContextFactory<RegistrationDbContext>
{
    public RegistrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=Streetcode;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsHistoryTable("__IdentityMigrationsHistory", "entity_framework"))
            .Options;
        return new RegistrationDbContext(options);
    }
}
