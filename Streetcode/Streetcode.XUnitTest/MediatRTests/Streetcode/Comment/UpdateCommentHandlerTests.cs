// <copyright file="UpdateCommentHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.Comments;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.Update;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;
    using CommentEntity = global::Streetcode.DAL.Entities.Streetcode.Comment;

    public class UpdateCommentHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();

        public UpdateCommentHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.CommentRepository)
                .Returns(this.commentRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCommentExists_ShouldUpdateTrimmedTextAndReturnDto()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "  Updated comment  ", RowVersion = new byte[] { 1 } });
            var comment = new CommentEntity { Id = command.Id, AuthorId = command.AuthorId, Text = "Old comment", RowVersion = new byte[] { 1 } };
            var expectedDto = new CommentDto { Id = command.Id, Text = "Updated comment" };
            this.SetupComment(command.Id, comment);
            this.mapperMock
                .Setup(mapper => mapper.Map<CommentDto>(comment))
                .Returns(expectedDto);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            var beforeUpdate = DateTimeOffset.UtcNow;

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            var afterUpdate = DateTimeOffset.UtcNow;
            Assert.True(result.IsSuccess);
            Assert.Same(expectedDto, result.Value);
            Assert.Equal("Updated comment", comment.Text);
            Assert.NotNull(comment.UpdatedAt);
            Assert.InRange(comment.UpdatedAt!.Value, beforeUpdate, afterUpdate);
            this.commentRepositoryMock.Verify(repository => repository.Update(comment), Times.Once());
            this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
            this.loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCommentDoesNotExist_ShouldReturnFailureAndNotSave()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } });
            const string expectedMessage = "Cannot find a comment with corresponding id: 15";
            this.SetupComment(command.Id, null);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.IsType<CommentNotFoundError>(result.Errors.Single());
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.commentRepositoryMock.Verify(repository => repository.Update(It.IsAny<CommentEntity>()), Times.Never());
            this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenSavingFails_ShouldReturnFailureAndLogError()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } });
            var comment = new CommentEntity { Id = command.Id, AuthorId = command.AuthorId, Text = "Old comment", RowVersion = new byte[] { 1 } };
            const string expectedMessage = "Failed to update comment with id: 15";
            this.SetupComment(command.Id, comment);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            Assert.Equal("Updated comment", comment.Text);
            this.commentRepositoryMock.Verify(repository => repository.Update(comment), Times.Once());
            this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCommentBelongsToAnotherUser_ShouldReturnFailureAndNotSave()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } });
            var comment = new CommentEntity { Id = command.Id, AuthorId = Guid.NewGuid(), Text = "Old comment", RowVersion = new byte[] { 1 } };
            const string expectedMessage = "You do not have permission to update comment with id: 15";
            this.SetupComment(command.Id, comment);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.IsType<CommentForbiddenError>(result.Errors.Single());
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            Assert.Equal("Old comment", comment.Text);
            Assert.Null(comment.UpdatedAt);
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.commentRepositoryMock.Verify(repository => repository.Update(It.IsAny<CommentEntity>()), Times.Never());
            this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCommentVersionIsOutdated_ShouldReturnConflictAndNotSave()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } });
            var comment = new CommentEntity { Id = command.Id, AuthorId = command.AuthorId, Text = "Old comment", RowVersion = new byte[] { 2 } };
            const string expectedMessage = "Comment with id: 15 was updated by another user.";
            this.SetupComment(command.Id, comment);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.IsType<CommentConflictError>(result.Errors.Single());
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.commentRepositoryMock.Verify(repository => repository.Update(It.IsAny<CommentEntity>()), Times.Never());
            this.repositoryWrapperMock.Verify(wrapper => wrapper.SaveChangesAsync(), Times.Never());
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
        }

        [Fact]
        public async Task Handle_WhenConcurrentSaveOccurs_ShouldReturnConflict()
        {
            var command = new UpdateCommentCommand(15, Guid.NewGuid(), new UpdateCommentDto { Text = "Updated comment", RowVersion = new byte[] { 1 } });
            var comment = new CommentEntity { Id = command.Id, AuthorId = command.AuthorId, Text = "Old comment", RowVersion = new byte[] { 1 } };
            const string expectedMessage = "Comment with id: 15 was updated by another user.";
            this.SetupComment(command.Id, comment);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.IsType<CommentConflictError>(result.Errors.Single());
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.commentRepositoryMock.Verify(repository => repository.Update(comment), Times.Once());
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.mapperMock.VerifyNoOtherCalls();
        }

        private UpdateCommentHandler CreateHandler() =>
            new (
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);

        private void SetupComment(int expectedId, CommentEntity? comment)
        {
            var expectedComment = comment ?? new CommentEntity { Id = expectedId };
            var decoyComment = new CommentEntity { Id = expectedId + 1 };
            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.Is<Expression<Func<CommentEntity, bool>>>(predicate =>
                        predicate.Compile().Invoke(expectedComment)
                        && !predicate.Compile().Invoke(decoyComment)),
                    null))
                .ReturnsAsync(comment);
        }
    }
}
