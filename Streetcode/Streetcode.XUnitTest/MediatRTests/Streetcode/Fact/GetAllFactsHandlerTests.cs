// <copyright file="GetAllFactsHandlerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Fact
{
    using AutoMapper;
    using global::Streetcode.BLL.DTO.Streetcode.TextContent.Fact;
    using global::Streetcode.BLL.MediatR.Streetcode.Fact.GetAll;
    using global::Streetcode.DAL.Repositories.Interfaces.Base;
    using global::Streetcode.DAL.Repositories.Interfaces.Streetcode.TextContent;
    using Microsoft.EntityFrameworkCore.Query;
    using Moq;
    using Xunit;
    using FactEntity = global::Streetcode.DAL.Entities.Streetcode.TextContent.Fact;

    public class GetAllFactsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> repositoryWrapperMock = new ();
        private readonly Mock<IFactRepository> factRepositoryMock = new ();
        private readonly Mock<IMapper> mapperMock = new ();

        public GetAllFactsHandlerTests()
        {
            this.repositoryWrapperMock
                .Setup(wrapper => wrapper.FactRepository)
                .Returns(this.factRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFactsOrderedByStreetcodeAndDisplayOrder()
        {
            var facts = new[]
            {
                new FactEntity { Id = 3, StreetcodeId = 2, DisplayOrder = 1 },
                new FactEntity { Id = 2, StreetcodeId = 1, DisplayOrder = 2 },
                new FactEntity { Id = 1, StreetcodeId = 1, DisplayOrder = 1 },
            };

            this.factRepositoryMock
                .Setup(repository => repository.GetAllAsync(
                    null,
                    It.IsAny<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>()))
                .ReturnsAsync(facts);
            this.mapperMock
                .Setup(mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()))
                .Returns((object source) => ((IEnumerable<FactEntity>)source)
                    .Select(fact => new FactDto { Id = fact.Id })
                    .ToList());

            var handler = new GetAllFactsHandler(
                this.repositoryWrapperMock.Object,
                this.mapperMock.Object);
            var result = await handler.Handle(
                new GetAllFactsQuery(),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(
                new[] { 1, 2, 3 },
                result.Value.Select(fact => fact.Id));

            this.factRepositoryMock.Verify(
                repository => repository.GetAllAsync(
                    null,
                    It.Is<Func<IQueryable<FactEntity>, IIncludableQueryable<FactEntity, object>>?>(
                        include => include != null)),
                Times.Once());
            this.mapperMock.Verify(
                mapper => mapper.Map<IEnumerable<FactDto>>(It.IsAny<object>()),
                Times.Once());
        }
    }
}
