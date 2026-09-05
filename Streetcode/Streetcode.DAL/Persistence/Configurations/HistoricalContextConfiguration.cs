using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.DAL.Entities.Timeline;

namespace Streetcode.DAL.Persistence.Configurations;

public class HistoricalContextConfiguration : IEntityTypeConfiguration<HistoricalContext>
{
    public void Configure(EntityTypeBuilder<HistoricalContext> builder)
    {
        builder
            .HasIndex(context => context.Title)
            .IsUnique();
    }
}