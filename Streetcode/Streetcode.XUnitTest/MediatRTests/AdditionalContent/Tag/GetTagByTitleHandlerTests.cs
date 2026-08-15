using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetByStreetcodeId;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetTagByTitle;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Tag
{
    public class GetTagByTitleHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetTagByTitleHandler _handler;

        public GetTagByTitleHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetTagByTitleHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsSuccessResult_WithCorrectType()
        {
            var query = new GetTagByTitleQuery("History");
            var tagEntity = new DAL.Entities.AdditionalContent.Tag { Id = 1, Title = "History" };
            var tagDto = new TagDTO { Id = 1, Title = "History" };

            _mockRepositoryWrapper.Setup(r => r.TagRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.AdditionalContent.Tag, bool>>>(), null))
                .ReturnsAsync(tagEntity);

            _mockMapper.Setup(m => m.Map<TagDTO>(It.IsAny<DAL.Entities.AdditionalContent.Tag>()))
                .Returns(tagDto);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsType<TagDTO>(result.Value);
            Assert.Equal(tagDto.Title, result.Value.Title);
        }

        [Fact]
        public async Task Handle_DataIsNull_ReturnsFailResultWithCorrectMessage_AndLogsError()
        {
            var query = new GetTagByTitleQuery("UnknownTitle");

            _mockRepositoryWrapper.Setup(r => r.TagRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.AdditionalContent.Tag, bool>>>(), null))
                .ReturnsAsync((DAL.Entities.AdditionalContent.Tag)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal($"Cannot find any tag by the title: {query.Title}", result.Errors[0].Message);
            _mockLogger.Verify(l => l.LogError(query, $"Cannot find any tag by the title: {query.Title}"), Times.Once);
        }
    }
}