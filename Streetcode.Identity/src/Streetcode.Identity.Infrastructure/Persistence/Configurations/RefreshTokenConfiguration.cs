using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.Identity.Domain.RefreshTokens;
using Streetcode.Identity.Infrastructure.Identity;

namespace Streetcode.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(
            "RefreshTokens",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_RefreshTokens_ExpiresAt_After_CreatedAt",
                    "[ExpiresAt] > [CreatedAt]");

                tableBuilder.HasCheckConstraint(
                    "CK_RefreshTokens_ConcurrencyVersion_Positive",
                    "[ConcurrencyVersion] >= 1");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .IsRequired(false);

        builder.Property(x => x.ReplacedByTokenId)
            .IsRequired(false);

        builder.Property(x => x.ConcurrencyVersion)
            .IsRequired()
            .HasDefaultValue(1L)
            .IsConcurrencyToken();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(x => x.FamilyId)
            .HasDatabaseName("IX_RefreshTokens_FamilyId");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
