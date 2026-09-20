using Streetcode.Identity.Application.IntegrationEvents;

namespace Streetcode.Identity.Application.Abstractions;

public interface IOutboxWriter
{
    Task AddAsync<TEvent>(
        TEvent integrationEvent,
        string key,
        CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}