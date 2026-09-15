// <copyright file="DeleteCommentHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using MediatR;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using CommentEntity = global::Streetcode.DAL.Entities.Streetcode.Comment;

    public class DeleteCommentHandlerTests
    {
        private static readonly int[] expectedDepthFirstReplyIds = { 19, 18, 17, 16 };

        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();

        public DeleteCommentHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.CommentRepository)
                .Returns(this.commentRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenRootCommentExists_ShouldDeleteRepliesAndComment()
        {
            var command = new DeleteCommentCommand(15);
            var replies = new List<CommentEntity>
            {
                new () { Id = 16, ParentCommentId = command.Id },
                new () { Id = 17, ParentCommentId = command.Id },
            };
            var comment = new CommentEntity
            {
                Id = command.Id,
            };
            this.SetupComment(command.Id, comment);
            this.SetupReplies(replies);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(3);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(
                    It.Is<IEnumerable<CommentEntity>>(deletedReplies =>
                        replies.All(deletedReplies.Contains) &&
                        deletedReplies.Count() == replies.Count)),
                Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(comment),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            this.loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenReplyExists_ShouldDeleteReply()
        {
            var command = new DeleteCommentCommand(16);
            var reply = new CommentEntity
            {
                Id = command.Id,
                ParentCommentId = 15,
            };
            this.SetupComment(command.Id, reply);
            this.SetupReplies(Array.Empty<CommentEntity>());
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(It.IsAny<IEnumerable<CommentEntity>>()),
                Times.Never());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(reply),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenRepliesAreNested_ShouldDeleteEntireSubtreeDepthFirst()
        {
            var command = new DeleteCommentCommand(15);
            var comment = new CommentEntity { Id = command.Id };
            var directReply = new CommentEntity { Id = 16, ParentCommentId = command.Id };
            var siblingReply = new CommentEntity { Id = 17, ParentCommentId = command.Id };
            var nestedReply = new CommentEntity { Id = 18, ParentCommentId = directReply.Id };
            var deepestReply = new CommentEntity { Id = 19, ParentCommentId = nestedReply.Id };
            this.SetupComment(command.Id, comment);
            this.SetupReplies(new[] { directReply, siblingReply, nestedReply, deepestReply });
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(5);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(
                    It.Is<IEnumerable<CommentEntity>>(replies =>
                        replies.Select(reply => reply.Id)
                            .SequenceEqual(expectedDepthFirstReplyIds))),
                Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(comment),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenCommentDoesNotExist_ShouldReturnFailureAndNotSave()
        {
            var command = new DeleteCommentCommand(404);
            const string expectedMessage = "Cannot find a comment with corresponding id: 404";
            this.SetupComment(command.Id, null);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedMessage),
                Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(It.IsAny<IEnumerable<CommentEntity>>()),
                Times.Never());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(It.IsAny<CommentEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        [Fact]
        public async Task Handle_WhenSavingFails_ShouldReturnFailureAndLogError()
        {
            var command = new DeleteCommentCommand(15);
            var comment = new CommentEntity { Id = command.Id };
            const string expectedMessage = "Failed to delete comment with id: 15";
            this.SetupComment(command.Id, comment);
            this.SetupReplies(Array.Empty<CommentEntity>());
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(comment),
                Times.Once());
            this.loggerMock.Verify(
                logger => logger.LogError(command, expectedMessage),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenCancellationIsRequested_ShouldNotQueryOrDelete()
        {
            var command = new DeleteCommentCommand(15);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                this.CreateHandler().Handle(command, cancellationTokenSource.Token));

            this.commentRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>()),
                Times.Never());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(It.IsAny<CommentEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
        }

        private void SetupComment(int expectedId, CommentEntity? comment)
        {
            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.Is<Expression<Func<CommentEntity, bool>>>(predicate =>
                        predicate.Compile()(new CommentEntity { Id = expectedId }) &&
                        !predicate.Compile()(new CommentEntity { Id = expectedId + 1 })),
                    It.Is<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>(include => include == null)))
                .ReturnsAsync(comment);
        }

        private void SetupReplies(IEnumerable<CommentEntity> comments)
        {
            this.commentRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>()))
                .ReturnsAsync((
                    Expression<Func<CommentEntity, bool>> predicate,
                    Func<IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>? _) =>
                    comments.Where(predicate.Compile()).ToList());
        }

        private DeleteCommentHandler CreateHandler()
        {
            return new DeleteCommentHandler(
                this.repositoryWrapperMock.Object,
                this.loggerMock.Object);
        }
    }
}
