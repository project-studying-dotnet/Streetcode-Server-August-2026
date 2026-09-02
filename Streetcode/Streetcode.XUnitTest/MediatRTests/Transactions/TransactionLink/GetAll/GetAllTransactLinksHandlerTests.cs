namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetAll
{
    using AutoMapper;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using global::Streetcode.BLL.DTO.Transactions;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Transactions.TransactionLink.GetAll;
    using global::Streetcode.DAL.Entities.Transactions;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection.Metadata;
    using System.Text;
    using Xunit;
    using TransactionLinkEntity = global::Streetcode.DAL.Entities.Transactions.TransactionLink;

    public class GetAllTransactLinksHandlerTests
    {
        private readonly Mock<IMapper> mapperMock;
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock;
        private readonly Mock<ILoggerService> loggerMock;
        private readonly GetAllTransactLinksHandler handler;

        public GetAllTransactLinksHandlerTests()
        {
            this.repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            this.mapperMock = new Mock<IMapper>();
            this.loggerMock = new Mock<ILoggerService>();

            this.handler = new GetAllTransactLinksHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResult_WhenTransactLinksExist()
        {
            var transactionLinks = GetTransactLinksList();
            var transactLinkDtos = GetTransactLinkDtosList();

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactionLinks);

            this.mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(transactionLinks))
                .Returns(transactLinkDtos);

            var query = new GetAllTransactLinksQuery();

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDtos);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailResult_WhenTransactLinksIsNull()
        {
            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((IEnumerable<TransactionLinkEntity>)null!);

            var query = new GetAllTransactLinksQuery();

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenTransactLinksIsNull()
        {
            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((IEnumerable<TransactionLinkEntity>)null!);

            var query = new GetAllTransactLinksQuery();

            await this.handler.Handle(query, CancellationToken.None);

            this.loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldCallMapperExactlyOnce_WhenTransactLinksExist()
        {
            var transactLinks = GetTransactLinksList();

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLinks);

            this.mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(transactLinks))
                .Returns(GetTransactLinkDtosList());

            var query = new GetAllTransactLinksQuery();

            await this.handler.Handle(query, CancellationToken.None);

            this.mapperMock.Verify(m => m.Map<IEnumerable<TransactLinkDTO>>(transactLinks), Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyCollection_WhenRepositoryReturnsEmptyList()
        {
            var emptyList = new List<TransactionLinkEntity>();

            this.repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(emptyList);

            this.mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(emptyList))
                .Returns(new List<TransactLinkDTO>());

            var query = new GetAllTransactLinksQuery();

            var result = await this.handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        private static List<TransactionLinkEntity> GetTransactLinksList()
        {
            return new List<TransactionLinkEntity>
            {
                new TransactionLinkEntity { Id = 1, Url = "https://example.com/1" },
                new TransactionLinkEntity { Id = 2, Url = "https://example.com/2" },
            };
        }

        private static List<TransactLinkDTO> GetTransactLinkDtosList()
        {
            return new List<TransactLinkDTO>
            {
                new TransactLinkDTO { Id = 1, Url = "https://example.com/1" },
                new TransactLinkDTO { Id = 2, Url = "https://example.com/2" },
            };
        }
    }
}
