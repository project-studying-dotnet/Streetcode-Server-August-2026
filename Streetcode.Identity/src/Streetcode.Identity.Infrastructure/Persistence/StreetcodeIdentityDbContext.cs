using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Domain.RefreshTokens;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence.Configurations;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.Infrastructure.Persistence;

public sealed class StreetcodeIdentityDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public StreetcodeIdentityDbContext(DbContextOptions<StreetcodeIdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var userBuilder = modelBuilder.Entity<ApplicationUser>();

        userBuilder.Property(x => x.Email)
            .IsRequired();

        userBuilder.Property(x => x.NormalizedEmail)
            .IsRequired();

        userBuilder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex");

        userBuilder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        userBuilder.Property(x => x.AccessVersion)
            .HasDefaultValue(1L)
            .IsConcurrencyToken();

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
