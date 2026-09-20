using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Application.EmailSending;

public sealed class MarkEmailDeliveryAsFailedHandler
{
    private readonly IEmailDeliveryRepository _repository;

    public MarkEmailDeliveryAsFailedHandler(IEmailDeliveryRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
    }

    public async Task HandleAsync(Guid messageId, CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("messageId cannot be empty", nameof(messageId));
        }

        var delivery = await _repository.GetByMessageIdAsync(messageId, cancellationToken);

        if (delivery is null)
        {
            throw new InvalidOperationException($"Unable to find an email delivery with id {messageId}");
        }

        if (delivery.Status != EmailDeliveryStatus.Pending)
        {
            return;
        }

        delivery.MarkAsFailed();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
