// <copyright file="DeleteSourceHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Sources.StreetcodeCategoryContent
{
    using System.Linq.Expressions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Delete;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Source;
    using MediatR;
    using Moq;
    using Xunit;
    using SourceEntity = global::Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

    public class DeleteSourceHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock = new();
        private readonly Mock<IStreetcodeCategoryContentRepository>
            _sourceRepositoryMock = new();
        private readonly Mock<ILoggerService> _loggerMock = new();

        public DeleteSourceHandlerTests()
        {
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.StreetcodeCategoryContentRepository)
                .Returns(_sourceRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSourceExists_ShouldDeleteSource()
        {
            var command = new DeleteSourceCommand(
                StreetcodeId: 1,
                SourceLinkCategoryId: 2);
            var source = new SourceEntity
            {
                StreetcodeId = command.StreetcodeId,
                SourceLinkCategoryId = command.SourceLinkCategoryId,
            };

            SetupSourceLookup(source);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(1);
            var handler = CreateHandler();

            var result = await handler.Handle(
                command,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(Unit.Value, result.Value);
            _sourceRepositoryMock.Verify(
                repository => repository.Delete(source),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenSourceDoesNotExist_ShouldReturnFailure()
        {
            var command = new DeleteSourceCommand(
                StreetcodeId: 1,
                SourceLinkCategoryId: 2);
            string expectedErrorMessage =
                "Cannot find source block for streetcode id: 1 and category id: 2";

            SetupSourceLookup(null);
            var handler = CreateHandler();

            var result = await handler.Handle(
                command,
                CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);
            _sourceRepositoryMock.Verify(
                repository => repository.Delete(It.IsAny<SourceEntity>()),
                Times.Never());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Never());
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedErrorMessage),
                Times.Once());
        }

        [Fact]
        public async Task Handle_WhenSaveFails_ShouldReturnFailure()
        {
            var command = new DeleteSourceCommand(
                StreetcodeId: 1,
                SourceLinkCategoryId: 2);
            var source = new SourceEntity
            {
                StreetcodeId = command.StreetcodeId,
                SourceLinkCategoryId = command.SourceLinkCategoryId,
            };
            const string expectedErrorMessage =
                "Failed to delete source block.";

            SetupSourceLookup(source);
            _repositoryWrapperMock
                .Setup(wrapper => wrapper.SaveChangesAsync())
                .ReturnsAsync(0);
            var handler = CreateHandler();

            var result = await handler.Handle(
                command,
                CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedErrorMessage, result.Errors.Single().Message);
            _sourceRepositoryMock.Verify(
                repository => repository.Delete(source),
                Times.Once());
            _repositoryWrapperMock.Verify(
                wrapper => wrapper.SaveChangesAsync(),
                Times.Once());
            _loggerMock.Verify(
                logger => logger.LogError(command, expectedErrorMessage),
                Times.Once());
        }

        private void SetupSourceLookup(SourceEntity? source)
        {
            _sourceRepositoryMock
                .Setup(repository => repository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<SourceEntity, bool>>>(),
                    null))
                .ReturnsAsync(source);
        }

        private DeleteSourceHandler CreateHandler()
        {
            return new DeleteSourceHandler(
                _repositoryWrapperMock.Object,
                _loggerMock.Object);
        }
    }
}
