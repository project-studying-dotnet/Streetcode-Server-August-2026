// <copyright file="GetCommentsToReviewHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment
{
    using System.Linq.Expressions;
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.Comments;
    using global::Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using CommentEntity = global::Streetcode.DAL.Entities.Streetcode.Comment;

    public class GetCommentsToReviewHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();

        public GetCommentsToReviewHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.CommentRepository)
                .Returns(this.commentRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnRequestedPageOfNewestRootComments()
        {
            var createdAt = new DateTimeOffset(2026, 9, 13, 18, 0, 0, TimeSpan.Zero);
            var rootComments = new List<CommentEntity>
            {
                CreateComment(1, createdAt.AddMinutes(-3)),
                CreateComment(5, createdAt),
                CreateComment(3, createdAt.AddMinutes(-1)),
                CreateComment(4, createdAt),
                CreateComment(2, createdAt.AddMinutes(-2)),
            };
            var request = new GetCommentsToReviewRequestDto
            {
                Page = 2,
                Amount = 2,
            };
            var query = new GetCommentsToReviewQuery(request);
            this.SetupRepository(rootComments);
            this.SetupMapping();

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Pages);
            Assert.Equal(
                new[] { 3, 2 },
                result.Value.Comments.Select(comment => comment.Id));
            this.commentRepositoryMock.Verify(
                repository => repository.FindAll(
                    It.Is<Expression<Func<CommentEntity, bool>>>(predicate =>
                        predicate.Compile()(new CommentEntity { ParentCommentId = null }) &&
                        !predicate.Compile()(new CommentEntity { ParentCommentId = 1 }))),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenThereAreNoRootComments_ShouldReturnEmptyFirstPage()
        {
            var query = new GetCommentsToReviewQuery(
                new GetCommentsToReviewRequestDto());
            this.SetupRepository(Array.Empty<CommentEntity>());
            this.SetupMapping();

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.Pages);
            Assert.Empty(result.Value.Comments);
        }

        [Fact]
        public async Task Handle_WhenPageIsOutsideAvailableRange_ShouldReturnEmptyPage()
        {
            var createdAt = new DateTimeOffset(2026, 9, 13, 18, 0, 0, TimeSpan.Zero);
            var query = new GetCommentsToReviewQuery(
                new GetCommentsToReviewRequestDto
                {
                    Page = int.MaxValue,
                    Amount = 100,
                });
            this.SetupRepository(new[]
            {
                CreateComment(1, createdAt),
                CreateComment(2, createdAt.AddMinutes(-1)),
            });
            this.SetupMapping();

            var result = await this.CreateHandler().Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.Pages);
            Assert.Empty(result.Value.Comments);
        }

        [Fact]
        public async Task Handle_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
        {
            var query = new GetCommentsToReviewQuery(
                new GetCommentsToReviewRequestDto());
            this.SetupRepository(Array.Empty<CommentEntity>());
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                this.CreateHandler().Handle(query, cancellationTokenSource.Token));
            this.commentRepositoryMock.Verify(
                repository => repository.FindAll(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>()),
                Times.Once());
        }

        private static CommentEntity CreateComment(int id, DateTimeOffset createdAt)
        {
            return new CommentEntity
            {
                Id = id,
                CreatedAt = createdAt,
            };
        }

        private void SetupRepository(IEnumerable<CommentEntity> comments)
        {
            this.commentRepositoryMock
                .Setup(repository => repository.FindAll(
                    It.IsAny<Expression<Func<CommentEntity, bool>>>()))
                .Returns(new TestAsyncEnumerable<CommentEntity>(comments));
        }

        private void SetupMapping()
        {
            this.mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<CommentDto>>(
                    It.IsAny<IEnumerable<CommentEntity>>()))
                .Returns((IEnumerable<CommentEntity> comments) => comments
                    .Select(comment => new CommentDto { Id = comment.Id })
                    .ToList());
        }

        private GetCommentsToReviewHandler CreateHandler()
        {
            return new GetCommentsToReviewHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object);
        }

        private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider inner;

            public TestAsyncQueryProvider(IQueryProvider inner)
            {
                this.inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestAsyncEnumerable<TElement>(expression);
            }

            public object? Execute(Expression expression)
            {
                return this.inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return this.inner.Execute<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(
                Expression expression,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Type resultType = typeof(TResult).GetGenericArguments().Single();
                object? result = this.inner.Execute(expression);
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult));

                if (fromResultMethod is null)
                {
                    throw new InvalidOperationException("Task.FromResult method was not found.");
                }

                object? taskResult = fromResultMethod
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new[] { result });

                return taskResult is TResult typedTaskResult
                    ? typedTaskResult
                    : throw new InvalidOperationException("Async query result has an unexpected type.");
            }
        }

        private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>,
            IAsyncEnumerable<T>,
            IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            public TestAsyncEnumerable(Expression expression)
                : base(expression)
            {
            }

            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

            public IAsyncEnumerator<T> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(
                    this.AsEnumerable().GetEnumerator(),
                    cancellationToken);
            }
        }

        private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> inner;
            private readonly CancellationToken cancellationToken;

            public TestAsyncEnumerator(
                IEnumerator<T> inner,
                CancellationToken cancellationToken)
            {
                this.inner = inner;
                this.cancellationToken = cancellationToken;
            }

            public T Current => this.inner.Current;

            public ValueTask<bool> MoveNextAsync()
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(this.inner.MoveNext());
            }

            public ValueTask DisposeAsync()
            {
                this.inner.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
