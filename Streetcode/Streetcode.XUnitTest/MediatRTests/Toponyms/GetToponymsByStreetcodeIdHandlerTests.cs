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
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.BLL.DTO.Toponyms;
using Streetcode.BLL.MediatR.Toponyms.GetByStreetcodeId;
using Streetcode.BLL.MediatR.Streetcode.Streetcode.GetById;
using Streetcode.BLL.MediatR.Toponyms.GetById;
using Microsoft.IdentityModel.Tokens;

namespace Streetcode.XUnitTest.MediatRTests.Toponyms
{
    public class GetToponymsByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<IToponymRepository> _toponymRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetToponymsByStreetcodeIdHandlerTests()
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
        public async Task Handle_WhenToponymsExist_ShouldReturnSuccess()
        {
            var toponyms = new List<Toponym>
            {
                new Toponym
                {
                    Id = 1,
                    StreetName = "First",
                    Streetcodes = { new StreetcodeContent { Id = 123 } },
                },
                new Toponym { Id = 2, StreetName = "Second" },
            };

            _toponymRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Toponym, bool>>>(predicate =>
                        predicate.Compile()(toponyms[0]) &&
                        !predicate.Compile()(new Toponym { Streetcodes = { new StreetcodeContent { Id = 999 } } })),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>?>()))
                .ReturnsAsync(toponyms);

            _mapperMock
                .Setup(mapper => mapper.Map<ToponymDTO>(It.IsAny<Toponym>()))
                .Returns<Toponym>(entity => new ToponymDTO
                {
                    Id = entity.Id,
                    StreetName = entity.StreetName,
                });

            var handler = new GetToponymsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetToponymsByStreetcodeIdQuery(123);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
            Assert.Contains(result.Value, dto => dto.Id == 1);
            Assert.Contains(result.Value, dto => dto.Id == 2);
        }

        [Fact]
        public async Task Handle_WhenToponymRepositoryReturnsEmptyList_ShouldReturnFailure()
        {
            _toponymRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>?>()))
                .ReturnsAsync(Array.Empty<Toponym>());

            var handler = new GetToponymsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetToponymsByStreetcodeIdQuery(12);
            var expectedMessage = "Cannot find any toponym by the streetcode id: 12";

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            _loggerMock.Verify(
                logger => logger.LogError(query, expectedMessage),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenToponymRepositoryReturnsNull_ShouldReturnFailure()
        {
            _toponymRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>?>()))
                .ReturnsAsync((IEnumerable<Toponym>)null!);

            var handler = new GetToponymsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );

            var query = new GetToponymsByStreetcodeIdQuery(12);
            var expectedMessage = "Cannot find any toponym by the streetcode id: 12";

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.Single().Message);
            _loggerMock.Verify(
                logger => logger.LogError(query, expectedMessage),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenToponymsContainDuplicateStreetNames_ShouldReturnDistinctResults()
        {
            var toponyms = new List<Toponym>
            {
                new Toponym { Id = 1, StreetName = "Shared" },
                new Toponym { Id = 2, StreetName = "Shared" },
                new Toponym { Id = 3, StreetName = "Unique" },
            };

            _toponymRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Toponym, bool>>>(),
                    It.IsAny<Func<IQueryable<Toponym>, IIncludableQueryable<Toponym, object>>?>()))
                .ReturnsAsync(toponyms);

            _mapperMock
                .Setup(mapper => mapper.Map<ToponymDTO>(It.IsAny<Toponym>()))
                .Returns<Toponym>(entity => new ToponymDTO
                {
                    Id = entity.Id,
                    StreetName = entity.StreetName,
                });

            var handler = new GetToponymsByStreetcodeIdHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetToponymsByStreetcodeIdQuery(1);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
            Assert.Contains(result.Value, dto => dto.StreetName == "Shared");
            Assert.Contains(result.Value, dto => dto.StreetName == "Unique");
        }
    }
}