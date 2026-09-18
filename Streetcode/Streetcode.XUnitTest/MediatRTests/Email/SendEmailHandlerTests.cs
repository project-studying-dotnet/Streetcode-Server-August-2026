using Microsoft.Extensions.Logging;
using Moq;
using Streetcode.BLL.DTO.Email;
using Streetcode.BLL.Exceptions;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.BLL.MediatR.Email;
using Streetcode.Email.Contracts.Events;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Email;

public class SendEmailHandlerTests
{
    private readonly Mock<IEmailRequestPublisher> _publisherMock = new();
    private readonly Mock<ILogger<SendEmailHandler>> _loggerMock = new();

    [Fact]
    public void Constructor_NullPublisher_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new SendEmailHandler(
                null!,
                _loggerMock.Object));

        Assert.Equal("emailRequestPublisher", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new SendEmailHandler(
                _publisherMock.Object,
                null!));

        Assert.Equal("logger", exception.ParamName);
    }

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

        var handler = new SendEmailHandler(
            _publisherMock.Object,
            _loggerMock.Object);
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
    public async Task Handle_PublisherFails_ReturnsFailure()
    {
        const string sender = "private-sender@example.com";
        const string content = "Private feedback content";
        var expectedException =
            new EmailRequestPublishingException(
                "Kafka unavailable.");

        var email = new EmailDTO
        {
            From = sender,
            Content = content,
        };
        var logger = new RecordingLogger<SendEmailHandler>();

        _publisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<EmailRequestedV1>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var handler = new SendEmailHandler(
            _publisherMock.Object,
            logger);

        var result = await handler.Handle(
            new SendEmailCommand(email),
            CancellationToken.None);

        Assert.True(result.IsFailed);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Unable to accept the email request.",
            error.Message);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, logEntry.Level);
        Assert.Same(expectedException, logEntry.Exception);
        Assert.DoesNotContain(sender, logEntry.Message);
        Assert.DoesNotContain(content, logEntry.Message);
    }

    [Fact]
    public async Task Handle_Canceled_PropagatesCancellationException()
    {
        var email = new EmailDTO
        {
            From = "loki@example.com",
            Content = "Some test info",
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var cancellationToken = cancellationTokenSource.Token;
        var cancellationException = new OperationCanceledException(
            cancellationToken);

        _publisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<EmailRequestedV1>(),
                cancellationToken))
            .ThrowsAsync(cancellationException);

        var handler = new SendEmailHandler(
            _publisherMock.Object,
            _loggerMock.Object);

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(
                new SendEmailCommand(email),
                cancellationToken));

        Assert.Same(cancellationException, exception);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(
                logLevel,
                formatter(state, exception),
                exception));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception);
}
