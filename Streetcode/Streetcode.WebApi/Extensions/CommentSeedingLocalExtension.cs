using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Persistence;

namespace Streetcode.WebApi.Extensions;

public static class CommentSeedingLocalExtension
{
    public static async Task SeedCommentsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StreetcodeDbContext>();

        if (dbContext.Comments.Any())
        {
            return;
        }

        var streetcode = dbContext.Streetcodes.FirstOrDefault();
        if (streetcode is null)
        {
            return;
        }

        dbContext.Comments.Add(CreateSampleComment(streetcode.Id));
        await dbContext.SaveChangesAsync();
    }

    public static Comment CreateSampleComment(int streetcodeId)
    {
        var authorId = Guid.Parse("8a17208f-d84b-42c0-bcbe-3331d3196f17");
        var comment = new Comment
        {
            StreetcodeId = streetcodeId,
            AuthorId = authorId,
            Text = "Дякую за цю історію!",
            CreatedAt = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero),
        };
        comment.Replies.Add(new Comment
        {
            StreetcodeId = streetcodeId,
            AuthorId = authorId,
            Text = "Раді, що вона вам сподобалася.",
            CreatedAt = new DateTimeOffset(2026, 9, 14, 11, 0, 0, TimeSpan.Zero),
        });

        return comment;
    }
}
