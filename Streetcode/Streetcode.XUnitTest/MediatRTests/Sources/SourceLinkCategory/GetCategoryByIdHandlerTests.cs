// <copyright file="GetCategoryByIdHandlerTests.cs" company="Streetcode">
// Copyright (c) Streetcode. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory
{
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Media.Images;
    using global::Streetcode.BLL.DTO.Sources;
    using global::Streetcode.BLL.Interfaces.BlobStorage;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Sources.SourceLink.GetCategoryById;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GetCategoryByIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock = new Mock<IRepositoryWrapper>();
        private readonly Mock<IMapper> mapperMock = new Mock<IMapper>();
        private readonly Mock<IBlobService> blobServiceMock = new Mock<IBlobService>();
        private readonly Mock<ILoggerService> loggerMock = new Mock<ILoggerService>();

        [Fact]
        public async Task Handle_ReturnsOkResult_WithValidData_WhenDataExists()
        {
            int id = 1;
            var category = new DAL.Entities.Sources.SourceLinkCategory() { Id = id, };
            var dto = new SourceLinkCategoryDTO()
            {
                Id = id,
                Image = new ImageDTO() { BlobName = "test.jpg", },
            };

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(category)),
                It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
                .ReturnsAsync(category);

            this.mapperMock.Setup(m => m.Map<SourceLinkCategoryDTO>(category))
                .Returns(dto);
            this.blobServiceMock.Setup(b => b.FindFileInStorageAsBase64("test.jpg"))
                .Returns("base64string");

            var handler = new GetCategoryByIdHandler(this.repositoryMock.Object, this.mapperMock.Object, this.blobServiceMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value.Id);
            this.blobServiceMock.Verify(b => b.FindFileInStorageAsBase64("test.jpg"), Times.Once);
            Assert.Equal("base64string", result.Value.Image.Base64);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenEntityNotFound()
        {
            int id = 1;
            var query = new GetCategoryByIdQuery(id);
            var expectedError = $"Cannot find any srcCategory by the corresponding id: {id}";
            var category = new DAL.Entities.Sources.SourceLinkCategory() { Id = id, };

            this.repositoryMock.Setup(r => r.SourceCategoryRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<DAL.Entities.Sources.SourceLinkCategory, bool>>>(expr => expr.Compile()(category)),
                It.IsAny<Func<IQueryable<DAL.Entities.Sources.SourceLinkCategory>, IIncludableQueryable<DAL.Entities.Sources.SourceLinkCategory, object>>>()))
                .ReturnsAsync((DAL.Entities.Sources.SourceLinkCategory)null!);

            var handler = new GetCategoryByIdHandler(this.repositoryMock.Object, this.mapperMock.Object, this.blobServiceMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        }
    }
}