// <copyright file="CreateReplyHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment
{
    using Ardalis.Specification;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.Comments;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.Delete;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.Reply;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using Moq;
    using Xunit;
    using CommentEntity = global::Streetcode.DAL.Entities.Streetcode.Comment;

    public class CreateReplyHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();

        public CreateReplyHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.CommentRepository)
                .Returns(this.commentRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenParentExists_ShouldCreateTrimmedReplyAndReturnDto()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "  Reply text  " });
            var parentComment = new CommentEntity { Id = command.ParentCommentId, StreetcodeId = 20 };
            var expectedDto = new CommentDto { Id = 25, Text = "Reply text" };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            CommentEntity? createdReply = null;
            this.SetupParentComment(command.ParentCommentId, parentComment, cancellationToken);
            this.commentRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<CommentEntity>()))
                .Callback<CommentEntity>(reply => createdReply = reply)
                .Returns((CommentEntity reply) => reply);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync(cancellationToken))
                .ReturnsAsync(1);
            this.mapperMock
                .Setup(mapper => mapper.Map<CommentDto>(It.IsAny<CommentEntity>()))
                .Returns(expectedDto);
            var beforeCreate = DateTimeOffset.UtcNow;

            var result = await this.CreateHandler().Handle(command, cancellationToken);

            var afterCreate = DateTimeOffset.UtcNow;
            Assert.True(result.IsSuccess);
            Assert.Same(expectedDto, result.Value);
            Assert.NotNull(createdReply);
            Assert.Equal(parentComment.Id, createdReply.ParentCommentId);
            Assert.Equal(parentComment.StreetcodeId, createdReply.StreetcodeId);
            Assert.Equal(command.AuthorId, createdReply.AuthorId);
            Assert.Equal("Reply text", createdReply.Text);
            Assert.InRange(createdReply.CreatedAt, beforeCreate, afterCreate);
            Assert.Null(createdReply.UpdatedAt);
            this.commentRepositoryMock.Verify(repository => repository.Create(createdReply), Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(cancellationToken),
                Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<CommentDto>(createdReply), Times.Once());
            this.loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenParentDoesNotExist_ShouldReturnFailureAndNotSave()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" });
            const string expectedMessage = "Cannot find a comment with corresponding id: 15";
            this.SetupParentComment(command.ParentCommentId, null);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.IsType<CommentNotFoundError>(result.Errors.Single());
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.commentRepositoryMock.Verify(repository => repository.Create(It.IsAny<CommentEntity>()), Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenParentIsReply_ShouldReturnFailureAndNotSave()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" });
            var parentReply = new CommentEntity
            {
                Id = command.ParentCommentId,
                StreetcodeId = 20,
                ParentCommentId = 10,
            };
            const string expectedMessage =
                "Cannot reply to comment with id: 15 because it is already a reply.";
            this.SetupParentComment(command.ParentCommentId, parentReply);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.Create(It.IsAny<CommentEntity>()),
                Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenSavingFails_ShouldReturnFailureAndNotMap()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" });
            var parentComment = new CommentEntity { Id = command.ParentCommentId, StreetcodeId = 20 };
            const string expectedMessage = "Failed to create reply for comment with id: 15";
            this.SetupParentComment(command.ParentCommentId, parentComment);
            this.commentRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<CommentEntity>()))
                .Returns((CommentEntity reply) => reply);
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(0);

            var result = await this.CreateHandler().Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(logger => logger.LogError(command, expectedMessage), Times.Once());
            this.commentRepositoryMock.Verify(
                repository => repository.Create(
                    It.Is<CommentEntity>(reply =>
                        reply.ParentCommentId == parentComment.Id &&
                        reply.StreetcodeId == parentComment.StreetcodeId)),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(CancellationToken.None),
                Times.Once());
            this.mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCancellationIsRequestedBeforeQuery_ShouldThrow()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" });
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                this.CreateHandler().Handle(command, cancellationTokenSource.Token));

            this.repositoryWrapperMock.VerifyGet(wrapper => wrapper.CommentRepository, Times.Never());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
            this.loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCancelledAfterCreatingReply_ShouldNotSave()
        {
            var command = new CreateReplyCommand(
                15,
                Guid.NewGuid(),
                new CreateCommentDto { Text = "Reply text" });
            var parentComment = new CommentEntity { Id = command.ParentCommentId, StreetcodeId = 20 };
            using var cancellationTokenSource = new CancellationTokenSource();
            this.SetupParentComment(
                command.ParentCommentId,
                parentComment,
                cancellationTokenSource.Token);
            this.commentRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<CommentEntity>()))
                .Callback(cancellationTokenSource.Cancel)
                .Returns((CommentEntity reply) => reply);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                this.CreateHandler().Handle(command, cancellationTokenSource.Token));

            this.commentRepositoryMock.Verify(
                repository => repository.Create(It.IsAny<CommentEntity>()),
                Times.Once());
            this.repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            this.mapperMock.VerifyNoOtherCalls();
            this.loggerMock.VerifyNoOtherCalls();
        }

        private CreateReplyHandler CreateHandler()
        {
            return new CreateReplyHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
        }

        private void SetupParentComment(
            int expectedId,
            CommentEntity? parentComment,
            CancellationToken cancellationToken = default)
        {
            var expectedComment = parentComment ?? new CommentEntity { Id = expectedId };
            var decoyComment = new CommentEntity { Id = expectedId + 1 };
            this.commentRepositoryMock
                .Setup(repository => repository.GetBySpecAsync(
                    It.Is<ISpecification<CommentEntity>>(specification =>
                        specification.Evaluate(new[] { expectedComment, decoyComment })
                            .Select(comment => comment.Id)
                            .SequenceEqual(new[] { expectedId })),
                    cancellationToken))
                .ReturnsAsync(parentComment);
        }
    }
}
