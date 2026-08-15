using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.GetAll;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Tag
{
    public class GetAllTagsHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly GetAllTagsHandler _handler;

        public GetAllTagsHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new GetAllTagsHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_DataExists_ReturnsSuccessResult_WithCorrectTypeAndCount()
        {
            var query = new GetAllTagsQuery();
            var tagsList = new List<DAL.Entities.AdditionalContent.Tag>
            {
                new DAL.Entities.AdditionalContent.Tag { Id = 1 },
                new DAL.Entities.AdditionalContent.Tag { Id = 2 }
            };
            var dtoList = new List<TagDTO>
            {
                new TagDTO { Id = 1 },
                new TagDTO { Id = 2 }
            };

            _mockRepositoryWrapper.Setup(r => r.TagRepository.GetAllAsync(null, null))
                .ReturnsAsync(tagsList);

            _mockMapper.Setup(m => m.Map<IEnumerable<TagDTO>>(It.IsAny<IEnumerable<DAL.Entities.AdditionalContent.Tag>>()))
                .Returns(dtoList);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsAssignableFrom<IEnumerable<TagDTO>>(result.Value);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task Handle_DataIsNull_ReturnsFailResultWithCorrectMessage_AndLogsError()
        {
            var query = new GetAllTagsQuery();

            _mockRepositoryWrapper.Setup(r => r.TagRepository.GetAllAsync(null, null))
                .ReturnsAsync((IEnumerable<DAL.Entities.AdditionalContent.Tag>)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal("Cannot find any tags", result.Errors[0].Message);
            _mockLogger.Verify(l => l.LogError(query, "Cannot find any tags"), Times.Once);
        }
    }
}
