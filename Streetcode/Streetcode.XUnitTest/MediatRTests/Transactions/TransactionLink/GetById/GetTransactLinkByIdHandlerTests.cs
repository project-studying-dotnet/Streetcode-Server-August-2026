namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetById
{
    using System;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using FluentAssertions;
    using global::Streetcode.BLL.DTO.Transactions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using TransactionLinkEntity = global::Streetcode.DAL.Entities.Transactions.TransactionLink;

    public class GetTransactLinkByIdHandlerTests
    {
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ILoggerService> loggerMock;
        private readonly GetTransactLinkByIdHandler handler;

        public GetTransactLinkByIdHandlerTests()
        {
            this.repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            this.mapperMock = new Mock<IMapper>();
            this.loggerMock = new Mock<ILoggerService>();

            this.handler = new GetTransactLinkByIdHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResult_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);
            var transactLinkDto = GetTransactLinkDto(id);

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(transactLinkDto);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDto);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailResult_WhenTransactLinkIsNull()
        {
            const int id = 1;

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldContainCorrectErrorMessage_WhenTransactLinkIsNull()
        {
            const int id = 42;
            var expectedErrorMsg = $"Cannot find any transaction link with corresponding id: {id}";

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.Errors[0].Message.Should().Be(expectedErrorMsg);
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenTransactLinkIsNull()
        {
            const int id = 1;

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            await this.handler.Handle(query, CancellationToken.None);

            this.loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldCallMapperExactlyOnce_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(GetTransactLinkDto(id));

            var query = new GetTransactLinkByIdQuery(id);

            await this.handler.Handle(query, CancellationToken.None);

            this.mapperMock.Verify(m => m.Map<TransactLinkDTO>(transactLink), Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldNotCallLogger_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    IdMatcher(id),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            this.mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(GetTransactLinkDto(id));

            var query = new GetTransactLinkByIdQuery(id);

            await this.handler.Handle(query, CancellationToken.None);

            this.loggerMock.Verify(
                l => l.LogError(It.IsAny<object>(), It.IsAny<string>()),
                Times.Never());
        }

        private static TransactionLinkEntity GetTransactLink(int id)
        {
            return new TransactionLinkEntity { Id = id, Url = $"https://example.com/{id}" };
        }

        private static TransactLinkDTO GetTransactLinkDto(int id)
        {
            return new TransactLinkDTO { Id = id, Url = $"https://example.com/{id}" };
        }

        private static Expression<Func<TransactionLinkEntity, bool>> IdMatcher(int expectedId)
        {
            var entityWithExpectedId = GetTransactLink(expectedId);
            var entityWithDifferentId = GetTransactLink(expectedId + 1);

            return It.Is<Expression<Func<TransactionLinkEntity, bool>>>(
                expr => expr.Compile()(entityWithExpectedId) && !expr.Compile()(entityWithDifferentId));
        }
    }
}