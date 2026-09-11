// <copyright file="HistoricalContextResolverTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.Services.Timeline
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.DTO.Timeline;
    using global::Streetcode.BLL.Services.Timeline;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Timeline;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using HistoricalContextEntity =
        global::Streetcode.DAL.Entities.Timeline.HistoricalContext;

    public class HistoricalContextResolverTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<IHistoricalContextRepository> historicalContextRepositoryMock = new ();
        private readonly HistoricalContextResolver resolver;

        public HistoricalContextResolverTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.HistoricalContextRepository)
                .Returns(this.historicalContextRepositoryMock.Object);

            this.resolver = new HistoricalContextResolver(
                this.repositoryWrapperMock.Object);
        }

        [Fact]
        public async Task ResolveAsync_WhenNewContextTitleIsMissing_ShouldReturnFailure()
        {
            var requestedContexts = new[]
            {
                new HistoricalContextDTO { Title = null },
            };

            var result = await this.resolver.ResolveAsync(requestedContexts);

            Assert.True(result.IsFailed);
            Assert.Equal(
                "Historical context title is required.",
                Assert.Single(result.Errors).Message);
            this.historicalContextRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ResolveAsync_WhenExistingContextIsMissing_ShouldReturnFailure()
        {
            const int missingContextId = 25;
            var requestedContexts = new[]
            {
                new HistoricalContextDTO { Id = missingContextId },
            };
            string expectedError =
                $"Cannot find historical contexts with IDs: {missingContextId}.";
            var matchingContext = new HistoricalContextEntity
            {
                Id = missingContextId,
            };
            var otherContext = new HistoricalContextEntity
            {
                Id = missingContextId + 1,
            };

            this.historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.Is<Expression<Func<HistoricalContextEntity, bool>>>(predicate =>
                        predicate.Compile()(matchingContext) &&
                        !predicate.Compile()(otherContext)),
                    It.IsAny<Func<
                        IQueryable<HistoricalContextEntity>,
                        IIncludableQueryable<HistoricalContextEntity, object>>?>()))
                .ReturnsAsync(Array.Empty<HistoricalContextEntity>());

            var result = await this.resolver.ResolveAsync(requestedContexts);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenNewContextTitleAlreadyExists_ShouldReturnFailure()
        {
            const int existingContextId = 7;
            const string contextTitle = "Culture";
            var requestedContexts = new[]
            {
                new HistoricalContextDTO { Id = existingContextId },
                new HistoricalContextDTO { Title = $" {contextTitle} " },
            };
            string expectedError =
                $"Historical contexts with titles already exist: {contextTitle}.";

            var conflictingContexts = new[]
            {
                new HistoricalContextEntity { Id = 1, Title = contextTitle },
            };

            this.SetupRepositoryQueries(
                existingContextId,
                contextTitle,
                new[]
                {
                    new HistoricalContextEntity { Id = existingContextId },
                },
                conflictingContexts);

            var result = await this.resolver.ResolveAsync(requestedContexts);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, Assert.Single(result.Errors).Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenContextsAreValid_ShouldReturnDistinctRelations()
        {
            const int existingContextId = 7;
            var requestedContexts = new[]
            {
                new HistoricalContextDTO { Id = existingContextId },
                new HistoricalContextDTO { Id = existingContextId },
                new HistoricalContextDTO { Title = " Culture " },
                new HistoricalContextDTO { Title = "culture" },
            };

            this.SetupRepositoryQueries(
                existingContextId,
                "Culture",
                new[]
                {
                    new HistoricalContextEntity { Id = existingContextId },
                },
                Array.Empty<HistoricalContextEntity>());

            var result = await this.resolver.ResolveAsync(requestedContexts);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(
                result.Value,
                relation => relation.HistoricalContextId == existingContextId);
            Assert.Contains(
                result.Value,
                relation => relation.HistoricalContext?.Title == "Culture");
        }

        private void SetupRepositoryQueries(
            int existingContextId,
            string newContextTitle,
            IEnumerable<HistoricalContextEntity> existingContexts,
            IEnumerable<HistoricalContextEntity> conflictingContexts)
        {
            var matchingIdContext = new HistoricalContextEntity
            {
                Id = existingContextId,
            };
            var otherIdContext = new HistoricalContextEntity
            {
                Id = existingContextId + 1,
            };
            var matchingTitleContext = new HistoricalContextEntity
            {
                Title = newContextTitle,
            };
            var otherTitleContext = new HistoricalContextEntity
            {
                Title = $"Other {newContextTitle}",
            };

            this.historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.Is<Expression<Func<HistoricalContextEntity, bool>>>(predicate =>
                        predicate.Compile()(matchingIdContext) &&
                        !predicate.Compile()(otherIdContext)),
                    It.IsAny<Func<
                        IQueryable<HistoricalContextEntity>,
                        IIncludableQueryable<HistoricalContextEntity, object>>?>()))
                .ReturnsAsync(existingContexts);
            this.historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.Is<Expression<Func<HistoricalContextEntity, bool>>>(predicate =>
                        predicate.Compile()(matchingTitleContext) &&
                        !predicate.Compile()(otherTitleContext)),
                    It.IsAny<Func<
                        IQueryable<HistoricalContextEntity>,
                        IIncludableQueryable<HistoricalContextEntity, object>>?>()))
                .ReturnsAsync(conflictingContexts);
        }
    }
}
