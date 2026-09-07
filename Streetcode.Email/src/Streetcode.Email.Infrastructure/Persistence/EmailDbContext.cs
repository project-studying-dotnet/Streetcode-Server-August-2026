using Microsoft.EntityFrameworkCore;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Infrastructure.Persistence;

public sealed class EmailDbContext : DbContext
{
    public EmailDbContext(DbContextOptions<EmailDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailDelivery> EmailDeliveries => Set<EmailDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EmailDbContext).Assembly);
    }
}
