using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Application.EmailSending;

public sealed class SendEmailDeliveryHandler
{
    private readonly IEmailDeliveryRepository repository;
    private readonly IEmailDeliverySender sender;

    public SendEmailDeliveryHandler(
        IEmailDeliveryRepository repository,
        IEmailDeliverySender sender)
    {
        this.repository = repository;
        this.sender = sender;
    }

    public async Task HandleAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Message ID cannot be empty.",
                nameof(messageId));
        }

        var delivery = await repository.GetByMessageIdAsync(
            messageId,
            cancellationToken);

        if (delivery is null)
        {
            throw new InvalidOperationException(
                $"Email delivery '{messageId}' was not found.");
        }

        if (delivery.Status != EmailDeliveryStatus.Pending)
        {
            return;
        }

        await sender.SendAsync(
            delivery,
            cancellationToken);

        delivery.MarkAsSent();

        await repository.SaveChangesAsync(cancellationToken);
    }
}
