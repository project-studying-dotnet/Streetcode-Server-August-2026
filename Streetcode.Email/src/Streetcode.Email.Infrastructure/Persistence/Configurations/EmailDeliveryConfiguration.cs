using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryConfiguration
    : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> builder)
    {
        builder.ToTable("EmailDeliveries");

        builder.HasKey(delivery => delivery.MessageId);

        builder.Property(delivery => delivery.MessageId)
            .ValueGeneratedNever();

        builder.Property(delivery => delivery.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(delivery => delivery.CorrelationId)
            .IsRequired();

        builder.Property(delivery => delivery.RequestedAtUtc)
            .IsRequired();

        builder.Property(delivery => delivery.Template)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(delivery => delivery.Recipient)
            .HasMaxLength(254);

        builder.Ignore(delivery => delivery.TemplateData);

        builder.Property<Dictionary<string, string>>("templateData")
            .HasField("templateData")
            .HasColumnName("TemplateData")
            .HasConversion(
                templateData => JsonSerializer.Serialize(
                    templateData,
                    JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(
                            json,
                            JsonSerializerOptions.Default)
                        ?? new Dictionary<string, string>(
                            StringComparer.Ordinal))
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasIndex(delivery => delivery.CorrelationId);
        builder.HasIndex(delivery => delivery.Status);
    }
}
