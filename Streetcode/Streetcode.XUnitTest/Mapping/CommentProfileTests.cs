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
                ParentCommentId = 15,
                AuthorId = Guid.Parse("b1ba3183-45cd-4988-a14d-0a9521e1df65"),
                Text = "A mapped comment.",
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddMinutes(10),
            };

            var result = mapper.Map<CommentDto>(comment);

            Assert.Equal(comment.Id, result.Id);
            Assert.Equal(comment.StreetcodeId, result.StreetcodeId);
            Assert.Equal(comment.ParentCommentId, result.ParentCommentId);
            Assert.Equal(comment.AuthorId, result.AuthorId);
            Assert.Equal(comment.Text, result.Text);
            Assert.Equal(comment.CreatedAt, result.CreatedAt);
            Assert.Equal(comment.UpdatedAt, result.UpdatedAt);
        }

        [Fact]
        public void Map_ShouldCopyCommentAndDirectRepliesToCommentWithRepliesDto()
        {
            var configuration = new MapperConfiguration(config => config.AddProfile<CommentProfile>());
            configuration.AssertConfigurationIsValid();
            var mapper = configuration.CreateMapper();
            var createdAt = new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero);
            var comment = new Comment
            {
                Id = 10,
                StreetcodeId = 7,
                AuthorId = Guid.Parse("ef21e77a-baa8-4c24-bf36-f1ee4ef65fb2"),
                Text = "Root comment.",
                CreatedAt = createdAt,
                Replies =
                {
                    new Comment
                    {
                        Id = 11,
                        StreetcodeId = 7,
                        ParentCommentId = 10,
                        AuthorId = Guid.Parse("b73003c7-ce0c-456c-80a3-98921e9b8e8d"),
                        Text = "Direct reply.",
                        CreatedAt = createdAt.AddMinutes(1),
                        Replies =
                        {
                            new Comment
                            {
                                Id = 12,
                                StreetcodeId = 7,
                                ParentCommentId = 11,
                                AuthorId = Guid.Parse("3469ccbb-6bf6-4db9-8d9b-bcc909931e94"),
                                Text = "Nested reply.",
                                CreatedAt = createdAt.AddMinutes(2),
                            },
                        },
                    },
                },
            };

            var result = mapper.Map<CommentWithRepliesDto>(comment);

            Assert.Equal(comment.Id, result.Id);
            Assert.Equal(comment.StreetcodeId, result.StreetcodeId);
            Assert.Equal(comment.AuthorId, result.AuthorId);
            Assert.Equal(comment.Text, result.Text);
            Assert.Equal(comment.CreatedAt, result.CreatedAt);

            var reply = Assert.Single(result.Replies);
            Assert.Equal(11, reply.Id);
            Assert.Equal(10, reply.ParentCommentId);
            Assert.Equal("Direct reply.", reply.Text);
            Assert.IsNotType<CommentWithRepliesDto>(reply);
        }
    }
}
