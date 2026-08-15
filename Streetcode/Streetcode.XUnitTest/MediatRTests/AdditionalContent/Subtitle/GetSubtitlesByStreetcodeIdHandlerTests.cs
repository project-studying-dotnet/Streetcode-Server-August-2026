using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Subtitles;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Subtitle.GetByStreetcodeId;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Subtitle
{
    public class GetSubtitlesByStreetcodeIdHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetSubtitlesByStreetcodeIdHandler _handler;

        public GetSubtitlesByStreetcodeIdHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetSubtitlesByStreetcodeIdHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsResultWithCorrectTypeAndValue()
        {
            var query = new GetSubtitlesByStreetcodeIdQuery(1);
            var subtitleEntity = new DAL.Entities.AdditionalContent.Subtitle { Id = 1, StreetcodeId = 1 };
            var subtitleDto = new SubtitleDTO { Id = 1 };

            _mockRepositoryWrapper.Setup(r => r.SubtitleRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.AdditionalContent.Subtitle, bool>>>(), null))
                .ReturnsAsync(subtitleEntity);

            _mockMapper.Setup(m => m.Map<SubtitleDTO>(It.IsAny<DAL.Entities.AdditionalContent.Subtitle>()))
                .Returns(subtitleDto);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.IsType<SubtitleDTO>(result.Value);
            Assert.Equal(subtitleDto.Id, result.Value.Id);
        }

        [Fact]
        public async Task Handle_DataIsNull_ReturnsResultWithNullValue()
        {
            var query = new GetSubtitlesByStreetcodeIdQuery(99);

            _mockRepositoryWrapper.Setup(r => r.SubtitleRepository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<DAL.Entities.AdditionalContent.Subtitle, bool>>>(), null))
                .ReturnsAsync((DAL.Entities.AdditionalContent.Subtitle)null);

            _mockMapper.Setup(m => m.Map<SubtitleDTO>(It.IsAny<DAL.Entities.AdditionalContent.Subtitle>()))
                .Returns((SubtitleDTO)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Null(result.Value);
        }
    }
}