using Hangfire;
using Streetcode.Email.Application.EmailSending;

namespace Streetcode.Email.Infrastructure.BackgroundJobs;

public sealed class MarkEmailDeliveryAsFailedJob
{
    private readonly MarkEmailDeliveryAsFailedHandler _handler;

    public MarkEmailDeliveryAsFailedJob(MarkEmailDeliveryAsFailedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
    }

    [AutomaticRetry(
        Attempts = 3,
        OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(
        "email-delivery:{0}",
        300)]
    public Task ExecuteAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return _handler.HandleAsync(messageId, cancellationToken);
    }
}
