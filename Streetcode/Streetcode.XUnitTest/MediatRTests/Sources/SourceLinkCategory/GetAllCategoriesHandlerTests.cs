// <copyright file="GetAllCategoriesHandlerTests.cs" company="Streetcode">
// Copyright (c) Streetcode. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory
{
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Media.Images;
    using global::Streetcode.BLL.DTO.Sources;
    using global::Streetcode.BLL.Interfaces.BlobStorage;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetAll;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GetAllCategoriesHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock = new Mock<IRepositoryWrapper>();
        private readonly Mock<IMapper> mapperMock = new Mock<IMapper>();
        private readonly Mock<IBlobService> blobServiceMock = new Mock<IBlobService>();
        private readonly Mock<ILoggerService> loggerMock = new Mock<ILoggerService>();

        [Fact]
        public async Task Handle_ReturnsOkResult_WithExpectedCount_WhenDataExists()
        {
            var categories = new List<DAL.Entities.Sources.SourceLinkCategory>()
            {
                new DAL.Entities.Sources.SourceLinkCategory() { Id = 1, },
            };
            var dtos = new List<SourceLinkCategoryDTO>()
            {
                new SourceLinkCategoryDTO()
                {
                    Id = 1,
                    Image = new ImageDTO() { BlobName = "test.jpg", },
                },
            };

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
                .ReturnsAsync(categories);
            this.mapperMock.Setup(m => m.Map<IEnumerable<SourceLinkCategoryDTO>>(categories))
                .Returns(dtos);
            this.blobServiceMock.Setup(b => b.FindFileInStorageAsBase64("test.jpg"))
                .Returns("base64string");

            var handler = new GetAllCategoriesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.blobServiceMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            this.blobServiceMock.Verify(b => b.FindFileInStorageAsBase64("test.jpg"), Times.Once);
            Assert.Equal("base64string", result.Value.First().Image.Base64);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenRepositoryReturnsNull()
        {
            var expectedError = "Categories is null";
            var query = new GetAllCategoriesQuery();

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
                .ReturnsAsync((IEnumerable<DAL.Entities.Sources.SourceLinkCategory>)null!);

            var handler = new GetAllCategoriesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.blobServiceMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        }

        [Fact]
        public async Task Handle_ReturnsOkResult_WithEmptyList_WhenRepositoryReturnsEmptyCollection()
        {
            var emptyList = new List<DAL.Entities.Sources.SourceLinkCategory>();
            var emptyDtos = new List<SourceLinkCategoryDTO>();

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetAllAsync(null, It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
                .ReturnsAsync(emptyList);
            this.mapperMock.Setup(m => m.Map<IEnumerable<SourceLinkCategoryDTO>>(emptyList))
                .Returns(emptyDtos);

            var handler = new GetAllCategoriesHandler(this.repositoryMock.Object, this.mapperMock.Object, this.blobServiceMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}