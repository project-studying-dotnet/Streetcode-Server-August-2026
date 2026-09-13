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

    public Task ExecuteAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return _handler.HandleAsync(messageId, cancellationToken);
    }
}
