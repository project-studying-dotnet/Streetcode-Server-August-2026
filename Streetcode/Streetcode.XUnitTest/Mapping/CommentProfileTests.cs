// <copyright file="CommentProfileTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Mapping
{
    using AutoMapper;
    using Streetcode.BLL.DTO.Streetcode.Comments;
    using Streetcode.BLL.Mapping.Streetcode;
    using Streetcode.DAL.Entities.Streetcode;
    using Xunit;

    public class CommentProfileTests
    {
        [Fact]
        public void Map_ShouldCopyAllCommentPropertiesToCommentDto()
        {
            var configuration = new MapperConfiguration(config => config.AddProfile<CommentProfile>());
            configuration.AssertConfigurationIsValid();
            var mapper = configuration.CreateMapper();
            var createdAt = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero);
            var comment = new Comment
            {
                Id = 42,
                StreetcodeId = 7,
                AuthorId = Guid.Parse("b1ba3183-45cd-4988-a14d-0a9521e1df65"),
                Text = "A mapped comment.",
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddMinutes(10),
            };

            var result = mapper.Map<CommentDto>(comment);

            Assert.Equal(comment.Id, result.Id);
            Assert.Equal(comment.StreetcodeId, result.StreetcodeId);
            Assert.Equal(comment.AuthorId, result.AuthorId);
            Assert.Equal(comment.Text, result.Text);
            Assert.Equal(comment.CreatedAt, result.CreatedAt);
            Assert.Equal(comment.UpdatedAt, result.UpdatedAt);
        }
    }
}
