using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetByStreetcodeId;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Tag
{
    public class GetTagByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetTagByStreetcodeIdHandler _handler;

        public GetTagByStreetcodeIdHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetTagByStreetcodeIdHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsSuccessResult_WithCorrectTypeAndCount()
        {
            var query = new GetTagByStreetcodeIdQuery(1);
            var indexList = new List<StreetcodeTagIndex>
            {
                new StreetcodeTagIndex { StreetcodeId = 1, TagId = 1, Index = 2 },
                new StreetcodeTagIndex { StreetcodeId = 1, TagId = 2, Index = 1 }
            };
            var dtoList = new List<StreetcodeTagDTO>
            {
                new StreetcodeTagDTO { Id = 2 },
                new StreetcodeTagDTO { Id = 1 }
            };

            _mockRepositoryWrapper.Setup(r => r.StreetcodeTagIndexRepository.GetAllAsync(
                It.IsAny<Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, IIncludableQueryable<StreetcodeTagIndex, object>>>()))
                .ReturnsAsync(indexList);

            _mockMapper.Setup(m => m.Map<IEnumerable<StreetcodeTagDTO>>(It.IsAny<IEnumerable<StreetcodeTagIndex>>()))
                .Returns(dtoList);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsAssignableFrom<IEnumerable<StreetcodeTagDTO>>(result.Value);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task Handle_DataIsNull_ReturnsFailResultWithCorrectMessage_AndLogsError()
        {
            var query = new GetTagByStreetcodeIdQuery(99);

            _mockRepositoryWrapper.Setup(r => r.StreetcodeTagIndexRepository.GetAllAsync(
                It.IsAny<Expression<Func<StreetcodeTagIndex, bool>>>(),
                It.IsAny<Func<IQueryable<StreetcodeTagIndex>, IIncludableQueryable<StreetcodeTagIndex, object>>>()))
                .ReturnsAsync((IEnumerable<StreetcodeTagIndex>)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal($"Cannot find any tag by the streetcode id: {query.StreetcodeId}", result.Errors[0].Message);
            _mockLogger.Verify(l => l.LogError(query, $"Cannot find any tag by the streetcode id: {query.StreetcodeId}"), Times.Once);
        }
    }
}
