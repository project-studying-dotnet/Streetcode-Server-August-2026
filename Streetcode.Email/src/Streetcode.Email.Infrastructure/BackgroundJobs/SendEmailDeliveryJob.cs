using Hangfire;
using Streetcode.Email.Application.EmailSending;

namespace Streetcode.Email.Infrastructure.BackgroundJobs;

public sealed class SendEmailDeliveryJob
{
    private readonly SendEmailDeliveryHandler _handler;

    public SendEmailDeliveryJob(SendEmailDeliveryHandler handler)
    {
        _handler = handler;
    }

    [AutomaticRetry(
        Attempts = 5,
        OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [DisableConcurrentExecution(
        "email-delivery:{0}",
        300)]
    public async Task ExecuteAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(messageId, cancellationToken);
    }
}
