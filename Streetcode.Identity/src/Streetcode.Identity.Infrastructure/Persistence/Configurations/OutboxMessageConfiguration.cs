using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt_OccurredAt");
    }
}
