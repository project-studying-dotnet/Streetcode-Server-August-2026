using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Infrastructure.Identity;

namespace Streetcode.Identity.Infrastructure.Persistence;

public sealed class StreetcodeIdentityDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public StreetcodeIdentityDbContext(DbContextOptions<StreetcodeIdentityDbContext> options)
        : base(options)
    {
    }

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
    }
}
