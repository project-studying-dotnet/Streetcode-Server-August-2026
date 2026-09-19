using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.DAL.Entities.HistoryMap;

namespace Streetcode.DAL.Persistence.Configurations;

public class HistoryMapRecordConfiguration : IEntityTypeConfiguration<HistoryMapRecord>
{
    public void Configure(EntityTypeBuilder<HistoryMapRecord> builder)
    {
        builder.ToTable("history_map_records", "streetcode");
        builder.HasKey(record => record.Id);

        builder.HasOne(record => record.Streetcode)
            .WithMany()
            .HasForeignKey(record => record.StreetcodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.Toponym)
            .WithMany()
            .HasForeignKey(record => record.ToponymId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
