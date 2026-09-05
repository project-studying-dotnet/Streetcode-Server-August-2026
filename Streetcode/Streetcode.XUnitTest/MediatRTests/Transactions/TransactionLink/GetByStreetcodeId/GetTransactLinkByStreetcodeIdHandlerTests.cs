namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetByStreetcodeId
{
    using System;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using FluentAssertions;
    using global::Streetcode.BLL.DTO.Transactions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.GetByStreetcodeId;
    using global::Streetcode.DAL.Entities.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using TransactionLinkEntity = global::Streetcode.DAL.Entities.Transactions.TransactionLink;

    public class GetTransactLinkByStreetcodeIdHandlerTests
    {
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ILoggerService> loggerMock;
        private readonly GetTransactLinkByStreetcodeIdHandler handler;

        public GetTransactLinkByStreetcodeIdHandlerTests()
        {
            this.mapperMock = new Mock<IMapper>();
            this.repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            this.loggerMock = new Mock<ILoggerService>();

            this.handler = new GetTransactLinkByStreetcodeIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResultWithValue_WhenTransactLinkExists()
        {
            const int streetcodeId = 1;
            var transactLink = GetTransactLink(streetcodeId);
            var transactLinkDto = GetTransactLinkDto(streetcodeId);

            this.SetupTransactLink(streetcodeId, transactLink);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(transactLink))
                .Returns(transactLinkDto);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDto);
        }

        [Fact]
        public async Task Handle_ShouldNotCallStreetcodeRepository_WhenTransactLinkExists()
        {
            const int streetcodeId = 1;
            var transactLink = GetTransactLink(streetcodeId);

            this.SetupTransactLink(streetcodeId, transactLink);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(transactLink))
                .Returns(GetTransactLinkDto(streetcodeId));

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await this.handler.Handle(query, CancellationToken.None);

            this.repositoryWrapperMock.Verify(
                r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_ShouldReturnFailResult_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 1;

            this.SetupTransactLink(streetcodeId, null);
            this.SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldContainCorrectErrorMessage_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 42;
            var expectedErrorMsg =
                $"Cannot find a transaction link by a streetcode id: {streetcodeId}, because such streetcode doesn`t exist";

            this.SetupTransactLink(streetcodeId, null);
            this.SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.Errors[0].Message.Should().Be(expectedErrorMsg);
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 1;

            this.SetupTransactLink(streetcodeId, null);
            this.SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await this.handler.Handle(query, CancellationToken.None);

            this.loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResultWithNullValue_WhenTransactLinkIsNullButStreetcodeExists()
        {
            const int streetcodeId = 1;
            var streetcode = new StreetcodeContent { Id = streetcodeId };

            this.SetupTransactLink(streetcodeId, null);
            this.SetupStreetcode(streetcodeId, streetcode);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(null))
                .Returns((TransactLinkDTO?)null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldNotLogError_WhenTransactLinkIsNullButStreetcodeExists()
        {
            const int streetcodeId = 1;
            var streetcode = new StreetcodeContent { Id = streetcodeId };

            this.SetupTransactLink(streetcodeId, null);
            this.SetupStreetcode(streetcodeId, streetcode);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(null))
                .Returns((TransactLinkDTO?)null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await this.handler.Handle(query, CancellationToken.None);

            this.loggerMock.Verify(
                l => l.LogError(It.IsAny<object>(), It.IsAny<string>()),
                Times.Never());
        }

        private static TransactionLinkEntity GetTransactLink(int streetcodeId)
        {
            return new TransactionLinkEntity { Id = 1, StreetcodeId = streetcodeId, Url = "https://example.com/1" };
        }

        private static TransactLinkDTO GetTransactLinkDto(int streetcodeId)
        {
            return new TransactLinkDTO { Id = 1, StreetcodeId = streetcodeId, Url = "https://example.com/1" };
        }

        private static Expression<Func<TransactionLinkEntity, bool>> TransactLinkStreetcodeIdMatcher(int streetcodeId)
        {
            var entityWithExpectedId = GetTransactLink(streetcodeId);
            var entityWithDifferentId = GetTransactLink(streetcodeId + 1);

            return It.Is<Expression<Func<TransactionLinkEntity, bool>>>(
                expr => expr.Compile()(entityWithExpectedId) && !expr.Compile()(entityWithDifferentId));
        }

        private static Expression<Func<StreetcodeContent, bool>> StreetcodeIdMatcher(int streetcodeId)
        {
            var entityWithExpectedId = new StreetcodeContent { Id = streetcodeId };
            var entityWithDifferentId = new StreetcodeContent { Id = streetcodeId + 1 };

            return It.Is<Expression<Func<StreetcodeContent, bool>>>(
                expr => expr.Compile()(entityWithExpectedId) && !expr.Compile()(entityWithDifferentId));
        }

        private void SetupTransactLink(int streetcodeId, TransactionLinkEntity? transactLink)
        {
            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    TransactLinkStreetcodeIdMatcher(streetcodeId),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);
        }

        private void SetupStreetcode(int streetcodeId, StreetcodeContent? streetcode)
        {
            this.repositoryWrapperMock
                .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    StreetcodeIdMatcher(streetcodeId),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(streetcode);
        }
    }
}