using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Streetcode.BLL.Exceptions;
using Streetcode.BLL.Interfaces.Email;
using Streetcode.Email.Contracts.Events;

namespace Streetcode.BLL.MediatR.Email;

public sealed class SendEmailHandler
    : IRequestHandler<SendEmailCommand, Result<Guid>>
{
    private const string FeedbackTemplate = "feedback.v1";
    private const string SenderEmailKey = "From";
    private const string ContentKey = "Content";

    private readonly IEmailRequestPublisher _emailRequestPublisher;
    private readonly ILogger<SendEmailHandler> _logger;

    public SendEmailHandler(
        IEmailRequestPublisher emailRequestPublisher,
        ILogger<SendEmailHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(emailRequestPublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _emailRequestPublisher = emailRequestPublisher;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        SendEmailCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messageId = request.Email.MessageId
                        ?? Guid.NewGuid();
        var emailRequested = new EmailRequestedV1(
            messageId,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            FeedbackTemplate,
            null,
            new Dictionary<string, string>
            {
                [SenderEmailKey] = request.Email.From,
                [ContentKey] = request.Email.Content,
            });

        try
        {
            await _emailRequestPublisher.PublishAsync(
                emailRequested,
                cancellationToken);
        }
        catch (EmailRequestPublishingException exception)
        {
            const string errorMessage =
                "Unable to accept the email request.";

            _logger.LogError(
                exception,
                "Failed to publish email request {MessageId} " +
                "with correlation {CorrelationId}.",
                emailRequested.MessageId,
                emailRequested.CorrelationId);

            return Result.Fail<Guid>(errorMessage);
        }

        return Result.Ok(messageId);
    }
}
