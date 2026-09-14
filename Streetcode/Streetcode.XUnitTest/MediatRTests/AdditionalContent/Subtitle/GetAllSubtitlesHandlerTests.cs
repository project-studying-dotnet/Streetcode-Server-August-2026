using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent.Subtitles;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Subtitle.GetAll;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Subtitle
{
    public class GetAllSubtitlesHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetAllSubtitlesHandler _handler;

        public GetAllSubtitlesHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetAllSubtitlesHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsSuccessResult_WithCorrectTypeAndCount()
        {
            var query = new GetAllSubtitlesQuery();
            var subtitlesList = new List<DAL.Entities.AdditionalContent.Subtitle>
            {
                new DAL.Entities.AdditionalContent.Subtitle { Id = 1 },
                new DAL.Entities.AdditionalContent.Subtitle { Id = 2 }
            };
            var dtoList = new List<SubtitleDTO>
            {
                new SubtitleDTO { Id = 1 },
                new SubtitleDTO { Id = 2 }
            };

            _mockRepositoryWrapper.Setup(r => r.SubtitleRepository.GetAllAsync(null, null))
                .ReturnsAsync(subtitlesList);

            _mockMapper.Setup(m => m.Map<IEnumerable<SubtitleDTO>>(It.IsAny<IEnumerable<DAL.Entities.AdditionalContent.Subtitle>>()))
                .Returns(dtoList);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsAssignableFrom<IEnumerable<SubtitleDTO>>(result.Value);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task Handle_DataIsNull_ReturnsFailResultWithCorrectMessage_AndLogsError()
        {
            var query = new GetAllSubtitlesQuery();

            _mockRepositoryWrapper.Setup(r => r.SubtitleRepository.GetAllAsync(null, null))
                .ReturnsAsync((IEnumerable<DAL.Entities.AdditionalContent.Subtitle>)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Cannot find any subtitles", result.Errors[0].Message);
            _mockLogger.Verify(l => l.LogError(query, "Cannot find any subtitles"), Times.Once);
        }
    }
}
