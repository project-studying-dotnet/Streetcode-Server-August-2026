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
    public class GetAllToponymItemsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<IToponymRepository> _toponymRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _loggerMock;

        public GetAllToponymItemsHandlerTests()
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
        public async Task Handle_WhenToponymItemsExist_ShouldReturnSuccess()
        {
            var toponymItems = new List<Toponym>
            {
                new Toponym { Id = 1, StreetName = "First Street" },
                new Toponym { Id = 2, StreetName = "Second Street" },
            }.AsQueryable();

            var expectedDtos = new List<ToponymDTO>
            {
                new ToponymDTO { Id = 1, StreetName = "First Street" },
                new ToponymDTO { Id = 2, StreetName = "Second Street" },
            };

            _toponymRepositoryMock
                .Setup(repo => repo.FindAll(null))
                .Returns(toponymItems);

            _mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<ToponymDTO>>(toponymItems.AsEnumerable()))
                .Returns(expectedDtos);

            var handler = new GetAllToponymsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetAllToponymsQuery(new GetAllToponymsRequestDTO());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.Pages);
            Assert.Equal(expectedDtos, result.Value.Toponyms);

            _toponymRepositoryMock.Verify(repo => repo.FindAll(null), Times.Once);
            _mapperMock.Verify(mapper => mapper.Map<IEnumerable<ToponymDTO>>(toponymItems.AsEnumerable()), Times.Once);
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenNoToponymItemsExist_ShouldReturnSuccessWithEmptyList()
        {
            var toponyms = new List<Toponym>().AsQueryable();
            var expectedDtos = new List<ToponymDTO>();

            _toponymRepositoryMock
                .Setup(repo => repo.FindAll(null))
                .Returns(toponyms);

            _mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<ToponymDTO>>(toponyms.AsEnumerable()))
                .Returns(expectedDtos);

            var handler = new GetAllToponymsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetAllToponymsQuery(new GetAllToponymsRequestDTO());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Toponyms);
            Assert.Equal(1, result.Value.Pages);
        }

        [Fact]
        public async Task Handle_WhenTitleProvided_ShouldFilterAndDistinctByStreetName()
        {
            var toponyms = new List<Toponym>
            {
                new Toponym { Id = 1, StreetName = "First Street" },
                new Toponym { Id = 2, StreetName = "first street" },
                new Toponym { Id = 3, StreetName = "Other Street" },
                new Toponym { Id = 4, StreetName = "fIrst strEet" },
                new Toponym { Id = 5, StreetName = "FirSt StreeT" },
            }.AsQueryable();

            var expectedDtos = new List<ToponymDTO>
            {
                new ToponymDTO { Id = 1, StreetName = "First Street" },
            };

            _toponymRepositoryMock
                .Setup(repo => repo.FindAll(null))
                .Returns(toponyms);

            //IEnumerable<Toponym>? capturedItems = null;

            _mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<ToponymDTO>>(It.IsAny<IEnumerable<Toponym>>()))

                // .Callback<object>(obj => capturedItems = (obj as IEnumerable<Toponym>)?.ToList()) //checks if the filtered items are passed to the mapper
                // (in case if use It.Is<IEnumerable<Toponym>>() with condition)
                .Returns(expectedDtos);

            var handler = new GetAllToponymsHandler(
                _repositoryWrapperMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var query = new GetAllToponymsQuery(new GetAllToponymsRequestDTO { Title = "first" });

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            //Assert.NotNull(capturedItems);
            //Assert.NotEmpty(capturedItems);
            Assert.Single(result.Value.Toponyms);
            Assert.Equal("First Street", result.Value.Toponyms.Single().StreetName);
        }
    }
}