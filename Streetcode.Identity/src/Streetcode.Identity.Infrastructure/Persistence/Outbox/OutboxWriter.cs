using System.Text.Json;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.IntegrationEvents;

namespace Streetcode.Identity.Infrastructure.Persistence.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly StreetcodeIdentityDbContext _dbContext;

    public OutboxWriter(StreetcodeIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync<TEvent>(
        TEvent integrationEvent,
        string key,
        CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var eventType = integrationEvent.GetType();
        var payload = JsonSerializer.Serialize(
            integrationEvent,
            eventType,
            SerializerOptions);

        var outboxMessage = new OutboxMessage(
            integrationEvent.EventId,
            eventType.Name,
            key,
            payload,
            integrationEvent.OccurredAt);

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
