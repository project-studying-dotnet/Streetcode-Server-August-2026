namespace Streetcode.Identity.Application.IntegrationEvents;

public sealed record UserAccessChangedV1(
    Guid EventId,
    Guid UserId,
    bool IsActive,
    long AccessVersion,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
