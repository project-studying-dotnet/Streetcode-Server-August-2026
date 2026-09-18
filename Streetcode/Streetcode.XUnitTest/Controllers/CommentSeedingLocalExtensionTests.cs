// <copyright file="CommentSeedingLocalExtensionTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Controllers
{
    using Moq;
    using Streetcode.DAL.Entities.Streetcode;
    using Streetcode.DAL.Repositories.Interfaces.Base;
    using Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using Streetcode.WebApi.Extensions;
    using Xunit;

    public class CommentSeedingLocalExtensionTests
    {
        [Fact]
        public async Task SeedCommentsAsync_WhenStreetcodeExists_CreatesCommentOnce()
        {
            var comments = new Mock<ICommentRepository>();
            var streetcodes = new Mock<IStreetcodeRepository>();
            var repositories = new Mock<IRepositoryWrapper>();
            comments.Setup(repository => repository.FindAll(null))
                .Returns(Array.Empty<Comment>().AsQueryable());
            streetcodes.Setup(repository => repository.FindAll(null))
                .Returns(new[] { new StreetcodeContent { Id = 42 } }.AsQueryable());
            repositories.Setup(wrapper => wrapper.CommentRepository).Returns(comments.Object);
            repositories.Setup(wrapper => wrapper.StreetcodeRepository).Returns(streetcodes.Object);
            repositories.Setup(wrapper => wrapper.SaveChangesAsync()).ReturnsAsync(1);

            await CommentSeedingLocalExtension.SeedCommentsAsync(repositories.Object);

            comments.Verify(
                repository => repository.Create(
                    It.Is<Comment>(comment => comment.StreetcodeId == 42 && comment.Replies.Count == 1)),
                Times.Once());
            repositories.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
        }

        [Fact]
        public async Task SeedCommentsAsync_WhenCommentsExist_DoesNotAddDuplicates()
        {
            var comments = new Mock<ICommentRepository>();
            var repositories = new Mock<IRepositoryWrapper>();
            comments.Setup(repository => repository.FindAll(null))
                .Returns(new[] { new Comment { Id = 1 } }.AsQueryable());
            repositories.Setup(wrapper => wrapper.CommentRepository).Returns(comments.Object);

            await CommentSeedingLocalExtension.SeedCommentsAsync(repositories.Object);

            comments.Verify(repository => repository.Create(It.IsAny<Comment>()), Times.Never());
            repositories.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task SeedCommentsAsync_WhenNoStreetcodeExists_DoesNotSave()
        {
            var comments = new Mock<ICommentRepository>();
            var streetcodes = new Mock<IStreetcodeRepository>();
            var repositories = new Mock<IRepositoryWrapper>();
            comments.Setup(repository => repository.FindAll(null))
                .Returns(Array.Empty<Comment>().AsQueryable());
            streetcodes.Setup(repository => repository.FindAll(null))
                .Returns(Array.Empty<StreetcodeContent>().AsQueryable());
            repositories.Setup(wrapper => wrapper.CommentRepository).Returns(comments.Object);
            repositories.Setup(wrapper => wrapper.StreetcodeRepository).Returns(streetcodes.Object);

            await CommentSeedingLocalExtension.SeedCommentsAsync(repositories.Object);

            repositories.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
        }

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
