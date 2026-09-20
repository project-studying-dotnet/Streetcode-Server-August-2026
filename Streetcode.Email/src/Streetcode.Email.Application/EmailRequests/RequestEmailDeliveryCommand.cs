namespace Streetcode.Email.Application.EmailRequests;

public sealed record RequestEmailDeliveryCommand(
    Guid MessageId,
    Guid CorrelationId,
    DateTimeOffset RequestedAtUtc,
    string Template,
    string? Recipient,
    IReadOnlyDictionary<string, string>? TemplateData);
