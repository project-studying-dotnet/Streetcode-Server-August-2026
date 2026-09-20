namespace Streetcode.Email.Contracts.Events;

public record EmailRequestedV1(
    Guid MessageId,
    Guid CorrelationId,
    DateTimeOffset RequestedAtUtc,
    string Template,
    string? Recipient,
    IReadOnlyDictionary<string, string> TemplateData);
