// <copyright file="GetCategoryContentByStreetcodeIdHandlerTests.cs" company="Streetcode">
// Copyright (c) Streetcode. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.MediatRTests.Sources.SourceLinkCategory
{
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Sources;
    using global::Streetcode.BLL.Interfaces.Logging;
    using global::Streetcode.BLL.MediatR.Sources.SourceLinkCategory.GetCategoryContentByStreetcodeId;
    using global::Streetcode.DAL.Entities.Streetcode;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using Moq;
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GetCategoryContentByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryMock = new Mock<IRepositoryWrapper>();
        private readonly Mock<IMapper> mapperMock = new Mock<IMapper>();
        private readonly Mock<ILoggerService> loggerMock = new Mock<ILoggerService>();

        [Fact]
        public async Task Handle_ReturnsOkResult_WithValidData_WhenBothStreetcodeAndContentExist()
        {
            int streetcodeId = 1;
            int categoryId = 1;
            var streetcode = new StreetcodeContent() { Id = streetcodeId, };
            var categoryContent = new DAL.Entities.Sources.StreetcodeCategoryContent() { StreetcodeId = streetcodeId, SourceLinkCategoryId = categoryId, };
            var dto = new StreetcodeCategoryContentDTO() { StreetcodeId = streetcodeId, SourceLinkCategoryId = categoryId, };

            this.repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeContent, bool>>>(expr => expr.Compile()(streetcode)), null))
                .ReturnsAsync(streetcode);

            this.repositoryMock.Setup(r => r.StreetcodeCategoryContentRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<DAL.Entities.Sources.StreetcodeCategoryContent, bool>>>(expr => expr.Compile()(categoryContent)), null))
                .ReturnsAsync(categoryContent);

            this.mapperMock.Setup(m => m.Map<StreetcodeCategoryContentDTO>(categoryContent))
                .Returns(dto);

            var handler = new GetCategoryContentByStreetcodeIdHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId), CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenStreetcodeNotFound()
        {
            int streetcodeId = 1;
            int categoryId = 1;
            var query = new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId);
            var expectedError = $"No such streetcode with id = {streetcodeId}";
            var streetcode = new StreetcodeContent() { Id = streetcodeId, };

            this.repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeContent, bool>>>(expr => expr.Compile()(streetcode)), null))
                .ReturnsAsync((StreetcodeContent)null!);

            var handler = new GetCategoryContentByStreetcodeIdHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        }

        [Fact]
        public async Task Handle_ReturnsFailedResult_AndLogsError_WhenStreetcodeExistsButContentIsNull()
        {
            int streetcodeId = 1;
            int categoryId = 1;
            var query = new GetCategoryContentByStreetcodeIdQuery(streetcodeId, categoryId);
            var expectedError = "The streetcode content is null";
            var streetcode = new StreetcodeContent() { Id = streetcodeId, };
            var categoryContent = new DAL.Entities.Sources.StreetcodeCategoryContent() { StreetcodeId = streetcodeId, SourceLinkCategoryId = categoryId, };

            this.repositoryMock.Setup(r => r.StreetcodeRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<StreetcodeContent, bool>>>(expr => expr.Compile()(streetcode)), null))
                .ReturnsAsync(streetcode);

            this.repositoryMock.Setup(r => r.StreetcodeCategoryContentRepository.GetFirstOrDefaultAsync(
                It.Is<Expression<Func<DAL.Entities.Sources.StreetcodeCategoryContent, bool>>>(expr => expr.Compile()(categoryContent)), null))
                .ReturnsAsync((DAL.Entities.Sources.StreetcodeCategoryContent)null!);

            var handler = new GetCategoryContentByStreetcodeIdHandler(this.repositoryMock.Object, this.mapperMock.Object, this.loggerMock.Object);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(expectedError, result.Errors.First().Message);
            this.loggerMock.Verify(l => l.LogError(query, expectedError), Times.Once);
        }
    }
}