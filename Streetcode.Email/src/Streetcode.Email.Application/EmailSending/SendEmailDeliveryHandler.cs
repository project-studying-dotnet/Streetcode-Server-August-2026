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

        if (delivery.Status == EmailDeliveryStatus.Sending)
        {
            delivery.MarkAsDeliveryUncertain();

            await repository.SaveChangesAsync(cancellationToken);

            return;
        }

        if (delivery.Status != EmailDeliveryStatus.Pending)
        {
            return;
        }

        delivery.MarkAsSending();

        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            await sender.SendAsync(
                delivery,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (EmailDeliveryOutcomeUnknownException)
        {
            delivery.MarkAsDeliveryUncertain();

            await repository.SaveChangesAsync(cancellationToken);

            return;
        }
        catch (Exception)
        {
            delivery.MarkAsPendingForRetry();

            await repository.SaveChangesAsync(cancellationToken);

            throw;
        }

        delivery.MarkAsSent();

        await repository.SaveChangesAsync(cancellationToken);
    }
}
