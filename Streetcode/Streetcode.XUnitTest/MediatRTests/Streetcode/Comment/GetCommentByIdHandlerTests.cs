// <copyright file="GetCommentByIdHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.Comments;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.GetById;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using CommentEntity = global::Streetcode.DAL.Entities.Streetcode.Comment;

    public class GetCommentByIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();
        private readonly Mock<ILoggerService> loggerMock = new ();

        public GetCommentByIdHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.CommentRepository)
                .Returns(this.commentRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCommentExists_ShouldReturnMappedCommentWithOrderedReplies()
        {
            var query = new GetCommentByIdQuery(15);
            var earlierDate = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);
            var comment = new CommentEntity
            {
                Id = query.Id,
                Replies =
                {
                    CreateReply(18, earlierDate.AddMinutes(1)),
                    CreateReply(17, earlierDate),
                    CreateReply(16, earlierDate),
                },
            };

            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>()))
                .ReturnsAsync(comment);

            this.mapperMock
                .Setup(mapper => mapper.Map<CommentWithRepliesDto>(comment))
                .Returns(() => new CommentWithRepliesDto
                {
                    Id = comment.Id,
                    Replies = comment.Replies
                        .Select(reply => new CommentDto { Id = reply.Id })
                        .ToList(),
                });

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(query.Id, result.Value.Id);
            Assert.Equal(new[] { 16, 17, 18 }, result.Value.Replies.Select(reply => reply.Id));

            this.commentRepositoryMock.Verify(
                repository => repository.GetFirstOrDefaultAsync(
                    It.Is<Expression<Func<CommentEntity, bool>>>(predicate =>
                        predicate.Compile()(new CommentEntity { Id = query.Id }) &&
                        !predicate.Compile()(new CommentEntity { Id = query.Id + 1 })),
                    It.Is<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>(include => include != null)),
                Times.Once());
            this.mapperMock.Verify(mapper => mapper.Map<CommentWithRepliesDto>(comment), Times.Once());
            this.loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenCommentHasNoReplies_ShouldReturnEmptyReplies()
        {
            var query = new GetCommentByIdQuery(20);
            var comment = new CommentEntity { Id = query.Id };
            var dto = new CommentWithRepliesDto { Id = query.Id };

            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>()))
                .ReturnsAsync(comment);
            this.mapperMock
                .Setup(mapper => mapper.Map<CommentWithRepliesDto>(comment))
                .Returns(dto);

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Replies);
        }

        [Fact]
        public async Task Handle_WhenCommentDoesNotExist_ShouldReturnFailureAndLogError()
        {
            var query = new GetCommentByIdQuery(404);
            const string expectedMessage = "Cannot find a comment with corresponding id: 404";

            this.commentRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<CommentEntity>,
                        IIncludableQueryable<CommentEntity, object>>?>()))
                .ReturnsAsync((CommentEntity?)null);

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            this.loggerMock.Verify(logger => logger.LogError(query, expectedMessage), Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<CommentWithRepliesDto>(It.IsAny<CommentEntity>()),
                Times.Never());
        }

        private static CommentEntity CreateReply(int id, DateTimeOffset createdAt)
        {
            return new CommentEntity
            {
                Id = id,
                CreatedAt = createdAt,
            };
        }

        private GetCommentByIdHandler CreateHandler()
        {
            return new GetCommentByIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
        }
    }
}
