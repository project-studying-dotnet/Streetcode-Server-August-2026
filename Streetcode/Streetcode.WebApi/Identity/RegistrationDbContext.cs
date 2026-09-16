using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Streetcode.WebApi.Identity;

public class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options)
    : IdentityDbContext<RegistrationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("Identity");
        builder.Entity<RegistrationUser>(entity =>
        {
            entity.Property(user => user.Name).HasMaxLength(50).IsRequired();
            entity.Property(user => user.Surname).HasMaxLength(50).IsRequired();
        });
    }
}
