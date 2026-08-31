using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Transactions;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;
using TransactionLinkEntity = Streetcode.DAL.Entities.Transactions.TransactionLink;

namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetById
{
    public class GetTransactLinkByIdHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly GetTransactLinkByIdHandler _handler;

        public GetTransactLinkByIdHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _loggerMock = new Mock<ILoggerService>();

            _handler = new GetTransactLinkByIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        private static TransactionLinkEntity GetTransactLink(int id = 1)
        {
            return new TransactionLinkEntity { Id = id, Url = $"https://example.com/{id}" };
        }

        private static TransactLinkDTO GetTransactLinkDto(int id = 1)
        {
            return new TransactLinkDTO { Id = id, Url = $"https://example.com/{id}" };
        }

        [Fact]
        public async void Handle_ShouldReturnSuccessResult_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);
            var transactLinkDto = GetTransactLinkDto(id);

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(transactLinkDto);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await _handler.Handle(query, CancellationToken.None);


            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDto);
        }

        [Fact]
        public async void Handle_ShouldReturnFailResult_WhenTransactLinkIsNull()
        {
            const int id = 1;

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldContainCorrectErrorMessage_WhenTransactLinkIsNull()
        {
            const int id = 14;
            var expectedErrorMsg = $"Cannot find any transaction link with corresponding id: {id}";


            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Errors[0].Message.Should().Be(expectedErrorMsg);
        }

        [Fact]
        public async void Handle_ShouldLogError_WhenTransactLinkIsNull()
        {
            const int id = 1;

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync((TransactionLinkEntity)null!);

            var query = new GetTransactLinkByIdQuery(id);

            await _handler.Handle(query, CancellationToken.None);

            _loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async void Handle_ShouldCallMapperExactlyOnce_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(GetTransactLinkDto(id));

            var query = new GetTransactLinkByIdQuery(id);


            await _handler.Handle(query, CancellationToken.None);

            _mapperMock.Verify(m => m.Map<TransactLinkDTO>(transactLink), Times.Once());
        }

        [Fact]
        public async void Handle_ShouldNotCallLogger_WhenTransactLinkExists()
        {
            const int id = 1;
            var transactLink = GetTransactLink(id);

            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO>(transactLink))
                .Returns(GetTransactLinkDto(id));

            var query = new GetTransactLinkByIdQuery(id);

            await _handler.Handle(query, CancellationToken.None);

            _loggerMock.Verify(
                l => l.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never());
        }
    }
}
