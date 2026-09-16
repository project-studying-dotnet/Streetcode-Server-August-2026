using FluentResults;
using MediatR;
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

    public SendEmailHandler(
        IEmailRequestPublisher emailRequestPublisher)
    {
        ArgumentNullException.ThrowIfNull(emailRequestPublisher);
        _emailRequestPublisher = emailRequestPublisher;
    }

    public async Task<Result<Guid>> Handle(
        SendEmailCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messageId = Guid.NewGuid();
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

        await _emailRequestPublisher.PublishAsync(
            emailRequested,
            cancellationToken);

        return Result.Ok(messageId);
    }
}
