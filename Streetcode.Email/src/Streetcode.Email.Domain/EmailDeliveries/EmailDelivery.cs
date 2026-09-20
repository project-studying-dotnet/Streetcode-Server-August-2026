namespace Streetcode.Email.Domain.EmailDeliveries;

public sealed class EmailDelivery
{
    private Dictionary<string, string> templateData =
        new(StringComparer.Ordinal);

    public Guid MessageId { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public string Template { get; private set; } = string.Empty;

    public string? Recipient { get; private set; }

    public IReadOnlyDictionary<string, string> TemplateData => templateData;

    public EmailDeliveryStatus Status { get; private set; }

    public bool IsJobScheduled { get; private set; }

    private EmailDelivery()
    {
    }

    public EmailDelivery(
        Guid messageId,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        string template,
        string? recipient,
        IReadOnlyDictionary<string, string> templateData)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Message ID cannot be empty.",
                nameof(messageId));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation ID cannot be empty.",
                nameof(correlationId));
        }

        if (requestedAtUtc == default ||
            requestedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Requested timestamp must be a valid UTC value.",
                nameof(requestedAtUtc));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(templateData);

        MessageId = messageId;
        CorrelationId = correlationId;
        RequestedAtUtc = requestedAtUtc;
        Template = template;
        Recipient = recipient;

        this.templateData = templateData.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        Status = EmailDeliveryStatus.Pending;
    }

    public void MarkJobAsScheduled()
    {
        IsJobScheduled = true;
    }

    public void MarkAsSending()
    {
        if (Status != EmailDeliveryStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending delivery can start sending.");
        }

        Status = EmailDeliveryStatus.Sending;
    }

    public void MarkAsPendingForRetry()
    {
        if (Status != EmailDeliveryStatus.Sending)
        {
            throw new InvalidOperationException(
                "Only a sending delivery can be retried.");
        }

        Status = EmailDeliveryStatus.Pending;
    }

    public void MarkAsSent()
    {
        if (Status != EmailDeliveryStatus.Sending)
        {
            throw new InvalidOperationException(
                "Only a sending delivery can be marked as sent.");
        }

        Status = EmailDeliveryStatus.Sent;
    }

    public void MarkAsDeliveryUncertain()
    {
        if (Status != EmailDeliveryStatus.Sending)
        {
            throw new InvalidOperationException(
                "Only a sending delivery can have an uncertain result.");
        }

        Status = EmailDeliveryStatus.DeliveryUncertain;
    }

    public void MarkAsFailed()
    {
        if (Status != EmailDeliveryStatus.Pending)
        {
            throw new InvalidOperationException(
                "Cannot mark as failed.");
        }

        Status = EmailDeliveryStatus.Failed;
    }
}
