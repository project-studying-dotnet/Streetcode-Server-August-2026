using Moq;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Email;
using Streetcode.DAL.Entities.AdditionalContent.Email;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Streetcode.XUnitTest.MediatRTests.Email
{
    public class SendEmailHandlerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly SendEmailHandler _handler;

        public SendEmailHandlerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILoggerService>();
            _handler = new SendEmailHandler(_emailServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenEmailServiceReturnsTrue_ShouldReturnSuccess()
        {
            var emailDTO = new EmailDTO { From = "loki@example.com", Content = "Some test info" };
            var sendEmailCommand = new SendEmailCommand(emailDTO);

            _emailServiceMock.Setup(service => service.SendEmailAsync(It.Is<Message>(m =>
                    m.To.Single().Address == "streetcodeua@gmail.com" &&
                    m.From == emailDTO.From &&
                    m.Subject == "FeedBack" &&
                    m.Content == emailDTO.Content
                 )))
                .ReturnsAsync(true);

            var result = await _handler.Handle(sendEmailCommand, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<SendEmailCommand>(), It.IsAny<string>())
            , Times.Never());
        }

        [Fact]
        public async Task Handle_WhenEmailServiceReturnsFalse_ShouldReturnFailure()
        {
            var emailDTO = new EmailDTO { From = "loki@example.com", Content = "Some test info" };
            var sendEmailCommand = new SendEmailCommand(emailDTO);

            _emailServiceMock.Setup(service => service.SendEmailAsync(It.Is<Message>(m =>
                    m.To.Single().Address == "streetcodeua@gmail.com" &&
                    m.From == emailDTO.From &&
                    m.Subject == "FeedBack" &&
                    m.Content == emailDTO.Content
                 )))
                .ReturnsAsync(false);

            var result = await _handler.Handle(sendEmailCommand, CancellationToken.None);

            Assert.True(result.IsFailed);

            Assert.Equal(1 , result.Errors.Count());

            _loggerMock.Verify(logger => logger.LogError(sendEmailCommand, It.Is<string>(s => !string.IsNullOrEmpty(s)))
            , Times.Once());
        }
    }
}