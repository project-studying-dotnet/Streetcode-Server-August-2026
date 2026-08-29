using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Transactions;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetAll;
using Streetcode.DAL.Entities.Transactions;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Text;
using Xunit;
using TransactionLinkEntity = Streetcode.DAL.Entities.Transactions.TransactionLink;

namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetAll
{
    public class GetAllTransactLinksHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly GetAllTransactLinksHandler _handler;

        public GetAllTransactLinksHandlerTests()
        {
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILoggerService>();

            _handler = new GetAllTransactLinksHandler(
                _repositoryWrapperMock.Object, 
                _mapperMock.Object, 
                _loggerMock.Object);
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

        [Fact]
        public async Task Handle_ShouldReturnSuccessResult_WhenTransactLinksExist()
        {
            var transactionLinks = GetTransactLinksList();
            var transactLinkDtos = GetTransactLinkDtosList();

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactionLinks);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(transactionLinks))
                .Returns(transactLinkDtos);

            var query = new GetAllTransactLinksQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDtos);
        }

        [Fact]
        public async void Handle_ShouldReturnFailResult_WhenTransactLinksIsNull()
        {
            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((IEnumerable<TransactionLinkEntity>)null!);

            var query = new GetAllTransactLinksQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async void Handle_ShouldLogError_WhenTransactLinksIsNull()
        {
            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((IEnumerable<TransactionLinkEntity>)null!);

            var query = new GetAllTransactLinksQuery();

            await _handler.Handle(query, CancellationToken.None);

            _loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async void Handle_ShouldCallMapperExactlyOnce_WhenTransactLinksExist()
        {
            var transactLinks = GetTransactLinksList();

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLinks);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(transactLinks))
                .Returns(GetTransactLinkDtosList());

            var query = new GetAllTransactLinksQuery();

            await _handler.Handle(query, CancellationToken.None);

            _mapperMock.Verify(m => m.Map<IEnumerable<TransactLinkDTO>>(transactLinks), Times.Once());
        }

        [Fact]
        public async void Handle_ShouldReturnEmptyCollection_WhenRepositoryReturnsEmptyList()
        {
            var emptyList = new List<TransactionLinkEntity>();

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(emptyList);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<TransactLinkDTO>>(emptyList))
                .Returns(new List<TransactLinkDTO>());

            var query = new GetAllTransactLinksQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
    }
}
