using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streetcode.DAL.Entities.Streetcode;

namespace Streetcode.DAL.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments", "streetcode");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Text)
            .IsRequired()
            .HasMaxLength(Comment.TextMaxLength);
        builder.HasOne(comment => comment.Streetcode)
            .WithMany()
            .HasForeignKey(comment => comment.StreetcodeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(comment => comment.ParentComment)
            .WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
