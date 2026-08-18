using AutoMapper;
using Moq;
using Streetcode.BLL.DTO.AdditionalContent;
using Streetcode.BLL.DTO.AdditionalContent.Tag;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.AdditionalContent.Tag.Create;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.AdditionalContent.Tag
{
    public class CreateTagHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _mockRepositoryWrapper;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly CreateTagHandler _handler;

        public CreateTagHandlerTests()
        {
            _mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
            _handler = new CreateTagHandler(_mockRepositoryWrapper.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidData_ReturnsSuccessResult_WithCorrectType()
        {
            var createTagDto = new CreateTagDTO { Title = "New Tag" };
            var query = new CreateTagQuery(createTagDto);
            var tagEntity = new DAL.Entities.AdditionalContent.Tag { Id = 1, Title = "New Tag" };

            _mockRepositoryWrapper.Setup(r => r.TagRepository.CreateAsync(It.IsAny<DAL.Entities.AdditionalContent.Tag>()))
                .ReturnsAsync(tagEntity);

            _mockRepositoryWrapper.Setup(r => r.SaveChanges());

            _mockMapper.Setup(m => m.Map<TagDTO>(It.IsAny<DAL.Entities.AdditionalContent.Tag>()))
                .Returns(new TagDTO { Id = 1, Title = "New Tag" });

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.IsType<TagDTO>(result.Value);
            Assert.Equal("New Tag", result.Value.Title);
        }

        [Fact]
        public async Task Handle_SaveChangesThrowsException_ReturnsFailResult_AndLogsError()
        {
            var createTagDto = new CreateTagDTO { Title = "Exception Tag" };
            var query = new CreateTagQuery(createTagDto);
            var exception = new Exception("Database error");

            _mockRepositoryWrapper.Setup(r => r.TagRepository.CreateAsync(It.IsAny<DAL.Entities.AdditionalContent.Tag>()))
                .ReturnsAsync(new DAL.Entities.AdditionalContent.Tag());

            _mockRepositoryWrapper.Setup(r => r.SaveChanges()).Throws(exception);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailed);
            Assert.Equal(exception.ToString(), result.Errors[0].Message);
            _mockLogger.Verify(l => l.LogError(query, exception.ToString()), Times.Once);
        }
    }
}
