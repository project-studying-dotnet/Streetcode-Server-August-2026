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
                Replies = replies,
            };
            this.SetupComment(command.Id, comment);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(3);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(replies),
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
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            this.commentRepositoryMock.Verify(
                repository => repository.DeleteRange(
                    It.Is<IEnumerable<CommentEntity>>(comments => !comments.Any())),
                Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.Delete(reply),
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

        private void SetupComment(int expectedId, CommentEntity? comment)
        {
            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.Is<Expression<Func<CommentEntity, bool>>>(predicate =>
                        predicate.Compile()(new CommentEntity { Id = expectedId }) &&
                        !predicate.Compile()(new CommentEntity { Id = expectedId + 1 })),
                    It.Is<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>(include => include != null)))
                .ReturnsAsync(comment);
        }

        private DeleteCommentHandler CreateHandler()
        {
            return new DeleteCommentHandler(
                this.repositoryWrapperMock.Object,
                this.loggerMock.Object);
        }
    }
}
