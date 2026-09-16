using Moq;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.BLL.MediatR.Email;
using Streetcode.Email.Contracts.Events;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Email;

public class SendEmailHandlerTests
{
    private readonly Mock<IEmailRequestPublisher> _publisherMock = new();

    [Fact]
    public async Task Handle_ValidEmail_PublishesFeedbackContract()
    {
        var email = new EmailDTO
        {
            From = "loki@example.com",
            Content = "Some test info",
        };
        var cancellationToken = new CancellationTokenSource().Token;
        EmailRequestedV1? publishedRequest = null;
        var before = DateTimeOffset.UtcNow;

        _publisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<EmailRequestedV1>(),
                cancellationToken))
            .Callback<EmailRequestedV1, CancellationToken>(
                (request, _) => publishedRequest = request)
            .Returns(Task.CompletedTask);

        var handler = new SendEmailHandler(_publisherMock.Object);
        var result = await handler.Handle(
            new SendEmailCommand(email),
            cancellationToken);
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.NotNull(publishedRequest);
        Assert.NotEqual(Guid.Empty, publishedRequest!.MessageId);
        Assert.Equal(publishedRequest.MessageId, result.Value);
        Assert.NotEqual(Guid.Empty, publishedRequest.CorrelationId);
        Assert.NotEqual(
            publishedRequest.MessageId,
            publishedRequest.CorrelationId);
        Assert.InRange(
            publishedRequest.RequestedAtUtc,
            before,
            after);
        Assert.Equal("feedback.v1", publishedRequest.Template);
        Assert.Null(publishedRequest.Recipient);
        Assert.Equal(2, publishedRequest.TemplateData.Count);
        Assert.Equal(email.From, publishedRequest.TemplateData["From"]);
        Assert.Equal(
            email.Content,
            publishedRequest.TemplateData["Content"]);

        _publisherMock.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<EmailRequestedV1>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PublisherFails_PropagatesFailure()
    {
        var expectedException = new InvalidOperationException(
            "Kafka unavailable.");
        var email = new EmailDTO
        {
            From = "loki@example.com",
            Content = "Some test info",
        };

        _publisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<EmailRequestedV1>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var handler = new SendEmailHandler(_publisherMock.Object);

        var actualException = await Assert.ThrowsAsync<
            InvalidOperationException>(
            () => handler.Handle(
                new SendEmailCommand(email),
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }
}
