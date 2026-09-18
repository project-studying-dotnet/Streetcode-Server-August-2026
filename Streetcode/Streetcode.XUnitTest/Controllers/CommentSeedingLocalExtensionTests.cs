// <copyright file="CommentSeedingLocalExtensionTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Controllers
{
    using Streetcode.WebApi.Extensions;
    using Xunit;

    public class CommentSeedingLocalExtensionTests
    {
        [Fact]
        public void CreateSampleComment_ShouldCreateReplyForRequestedStreetcode()
        {
            var comment = CommentSeedingLocalExtension.CreateSampleComment(42);

            Assert.Equal(42, comment.StreetcodeId);
            Assert.Null(comment.ParentCommentId);
            var reply = Assert.Single(comment.Replies);
            Assert.Equal(42, reply.StreetcodeId);
            Assert.Equal(comment.AuthorId, reply.AuthorId);
            Assert.True(reply.CreatedAt > comment.CreatedAt);
        }
    }
}
