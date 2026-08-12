using AutoMapper;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Toponyms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Streetcode.BLL.MediatR.Timeline.TimelineItem.GetAll;
using Streetcode.BLL.MediatR.Toponyms.GetAll;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Toponyms.GetByStreetcodeId;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetById;
using Streetcode.BLL.MediatR.Toponyms.GetById;
using Microsoft.AspNetCore.Http.Features;

namespace Streetcode.XUnitTest.MediatRTests.Toponyms
{
    public class GetToponymByIdHandlerTest
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<IToponymRepository> _toponymRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetToponymByIdHandlerTest()
        {
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _toponymRepositoryMock = new Mock<IToponymRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILoggerService>();

            _repositoryWrapperMock
                .Setup(wrapper => wrapper.ToponymRepository)
                .Returns(_toponymRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenToponymExists_ShouldReturnSuccess()
        {
            var toponym = new Toponym { Id = 42, StreetName = "Test Street" };
            var expectedDto = new ToponymDTO { Id = 42, StreetName = "Test Street" };

            _toponymRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    null))
                .ReturnsAsync(toponym);

            _mapperMock
                .Setup(mapper => mapper.Map<ToponymDTO>(toponym))
                .Returns(expectedDto);

            var handler = new GetToponymByIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetToponymByIdQuery(42);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedDto, result.Value);
        }

        [Fact]
        public async Task Handle_WhenToponymDoesNotExist_ShouldReturnFailure()
        {
            _toponymRepositoryMock
                .Setup(repo => repo.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    null))
                .ReturnsAsync((Toponym?)null);

            var handler = new GetToponymByIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetToponymByIdQuery(42);
            var expectedMessage = "Cannot find any toponym with corresponding id: 42";

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            _loggerMock.Verify(
                logger => logger.LogError(query, expectedMessage),
                Times.Once);
        }
    }
}