// <copyright file="GetAllCategoryNamesHandlerTests.cs" company="Streetcode">
// Copyright (c) Streetcode. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory
{
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Sources;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetAll;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Moq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GetAllCategoryNamesHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock = new Mock<IRepositoryWrapper>();
        private readonly Mock<IMapper> mapperMock = new Mock<IMapper>();
        private readonly Mock<ILoggerService> loggerMock = new Mock<ILoggerService>();

        [Fact]
        public async Task Handle_ReturnsOkResult_WithExpectedCount_WhenDataExists()
        {
            var categories = new List<DAL.Entities.Sources.SourceLinkCategory>()
            {
                new DAL.Entities.Sources.SourceLinkCategory() { Id = 1, Title = "Test", },
            };
            var dtos = new List<CategoryWithNameDTO>()
            {
                new CategoryWithNameDTO() { Id = 1, Title = "Test", },
            };

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, null))
                .ReturnsAsync(categories);
            this.mapperMock.Setup(m => m.Map<IEnumerable<CategoryWithNameDTO>>(categories))
                .Returns(dtos);

            var handler = new GetAllCategoryNamesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetAllCategoryNamesQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenRepositoryReturnsNull()
        {
            var expectedError = "Categories is null";
            var query = new GetAllCategoryNamesQuery();

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, null))
                .ReturnsAsync((IEnumerable<DAL.Entities.Sources.SourceLinkCategory>)null!);

            var handler = new GetAllCategoryNamesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        }

        [Fact]
        public async Task Handle_ReturnsOkResult_WithEmptyList_WhenRepositoryReturnsEmptyCollection()
        {
            var emptyList = new List<DAL.Entities.Sources.SourceLinkCategory>();
            var emptyDtos = new List<CategoryWithNameDTO>();

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, null))
                .ReturnsAsync(emptyList);
            this.mapperMock.Setup(m => m.Map<IEnumerable<CategoryWithNameDTO>>(emptyList))
                .Returns(emptyDtos);

            var handler = new GetAllCategoryNamesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetAllCategoryNamesQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}