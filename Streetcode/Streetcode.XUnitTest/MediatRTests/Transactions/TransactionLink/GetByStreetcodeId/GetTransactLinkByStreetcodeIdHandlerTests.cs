using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Transactions;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetById;
using Streetcode.BLL.MediatR.Transactions.TransactionLink.GetByStreetcodeId;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;
using TransactionLinkEntity = Streetcode.DAL.Entities.Transactions.TransactionLink;

namespace Streetcode.XUnitTest.MediatRTests.Transactions.TransactionLink.GetByStreetcodeId
{
    public class GetTransactLinkByStreetcodeIdHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly GetTransactLinkByStreetcodeIdHandler _handler;

        public GetTransactLinkByStreetcodeIdHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _loggerMock = new Mock<ILoggerService>();

            _handler = new GetTransactLinkByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        private static TransactionLinkEntity GetTransactLink(int streetcodeId = 1)
        {
            return new TransactionLinkEntity { Id = 1, StreetcodeId = streetcodeId, Url = "https://example.com/1" };
        }

        private static TransactLinkDTO GetTransactLinkDto(int streetcodeId = 1)
        {
            return new TransactLinkDTO { Id = 1, StreetcodeId = streetcodeId, Url = "https://example.com/1" };
        }

        private void SetupTransactLink(int streetcodeId, TransactionLinkEntity? transactLink)
        {
            _repositoryWrapperMock
                .Setup(r => r.TransactLinksRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<TransactionLinkEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionLinkEntity>, IIncludableQueryable<TransactionLinkEntity, object>>>()))
                .ReturnsAsync(transactLink);
        }

        private void SetupStreetcode(int streetcodeId, StreetcodeContent? streetcode)
        {
            _repositoryWrapperMock
                .Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()))
                .ReturnsAsync(streetcode);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResultWithValue_WhenTransactLinkExists()
        {
            const int streetcodeId = 1;
            var transactLink = GetTransactLink(streetcodeId);
            var transactLinkDto = GetTransactLinkDto(streetcodeId);

            SetupTransactLink(streetcodeId, transactLink);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(transactLink))
                .Returns(transactLinkDto);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(transactLinkDto);
        }

        [Fact]
        public async Task Handle_ShouldNotCallStreetcodeRepository_WhenTransactLinkExists()
        {
            const int streetcodeId = 1;
            var transactLink = GetTransactLink(streetcodeId);

            SetupTransactLink(streetcodeId, transactLink);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(transactLink))
                .Returns(GetTransactLinkDto(streetcodeId));

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await _handler.Handle(query, CancellationToken.None);

            _repositoryWrapperMock.Verify(
                r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<StreetcodeContent, bool>>>(),
                    It.IsAny<Func<IQueryable<StreetcodeContent>, IIncludableQueryable<StreetcodeContent, object>>>()),
                Times.Never());
        }

        [Fact]
        public async Task Handle_ShouldReturnFailResult_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 1;

            SetupTransactLink(streetcodeId, null);
            SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldContainCorrectErrorMessage_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 42;
            var expectedErrorMsg = $"Cannot find a transaction link by a streetcode id: {streetcodeId}, because such streetcode doesn`t exist";

            SetupTransactLink(streetcodeId, null);
            SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Errors[0].Message.Should().Be(expectedErrorMsg);
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenTransactLinkIsNullAndStreetcodeDoesNotExist()
        {
            const int streetcodeId = 1;

            SetupTransactLink(streetcodeId, null);
            SetupStreetcode(streetcodeId, null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await _handler.Handle(query, CancellationToken.None);

            _loggerMock.Verify(
                l => l.LogError(query, It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessResultWithNullValue_WhenTransactLinkIsNullButStreetcodeExists()
        {
            const int streetcodeId = 1;
            var streetcode = new StreetcodeContent { Id = streetcodeId };

            SetupTransactLink(streetcodeId, null);
            SetupStreetcode(streetcodeId, streetcode);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(null))
                .Returns((TransactLinkDTO?)null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldNotLogError_WhenTransactLinkIsNullButStreetcodeExists()
        {
            const int streetcodeId = 1;
            var streetcode = new StreetcodeContent { Id = streetcodeId };

            SetupTransactLink(streetcodeId, null);
            SetupStreetcode(streetcodeId, streetcode);

            _mapperMock
                .Setup(m => m.Map<TransactLinkDTO?>(null))
                .Returns((TransactLinkDTO?)null);

            var query = new GetTransactLinkByStreetcodeIdQuery(streetcodeId);

            await _handler.Handle(query, CancellationToken.None);

            _loggerMock.Verify(
                l => l.LogError(It.IsAny<object>(), It.IsAny<string>()),
                Times.Never());
        }
    }
}
