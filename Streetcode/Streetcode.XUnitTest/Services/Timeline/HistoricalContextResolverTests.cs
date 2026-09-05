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

            this.historicalContextRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
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
            const string contextTitle = "Culture";
            var requestedContexts = new[]
            {
                new HistoricalContextDTO { Title = $" {contextTitle} " },
            };
            string expectedError =
                $"Historical contexts with titles already exist: {contextTitle}.";

            var conflictingContexts = new[]
            {
                new HistoricalContextEntity { Id = 1, Title = contextTitle },
            };

            this.SetupRepositorySequence(
                Array.Empty<HistoricalContextEntity>(),
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

            this.SetupRepositorySequence(
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

        private void SetupRepositorySequence(
            IEnumerable<HistoricalContextEntity> existingContexts,
            IEnumerable<HistoricalContextEntity> conflictingContexts)
        {
            this.historicalContextRepositoryMock
                .SetupSequence(repository => repository.GetAllAsync(
                    It.IsAny<Expression<Func<HistoricalContextEntity, bool>>>(),
                    It.IsAny<Func<
                        IQueryable<HistoricalContextEntity>,
                        IIncludableQueryable<HistoricalContextEntity, object>>?>()))
                .ReturnsAsync(existingContexts)
                .ReturnsAsync(conflictingContexts);
        }
    }
}
