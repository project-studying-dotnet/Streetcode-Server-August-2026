using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Streetcode.Identity.Application.Abstractions;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.IntegrationTests.Fixtures;

namespace Streetcode.Identity.IntegrationTests.Persistence.Outbox;

[Collection(MsSqlCollection.Name)]
public sealed class OutboxWriterIntegrationTests
    : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public OutboxWriterIntegrationTests(MsSqlContainerFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(fixture.ConnectionString);

        _serviceProvider =
            services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task AddAsync_WhenSaved_ShouldPersistOutboxMessage()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var writer =
            scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

        var context =
            scope.ServiceProvider
                .GetRequiredService<StreetcodeIdentityDbContext>();

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(
            2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

        IIntegrationEvent integrationEvent = new UserAccessChangedV1(
            eventId,
            userId,
            false,
            2,
            occurredAt);

        await writer.AddAsync(
            integrationEvent,
            userId.ToString(),
            CancellationToken.None);

        var savedEntries = await context.SaveChangesAsync();

        Assert.Equal(1, savedEntries);
        context.ChangeTracker.Clear();

        var persistedMessage = await context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.Id == eventId);

        Assert.Equal(eventId, persistedMessage.Id);
        Assert.Equal("UserAccessChangedV1", persistedMessage.Type);
        Assert.Equal(userId.ToString(), persistedMessage.Key);
        Assert.Equal(occurredAt, persistedMessage.OccurredAt);
        Assert.False(string.IsNullOrWhiteSpace(persistedMessage.Payload));
        Assert.Null(persistedMessage.ProcessedAt);
        Assert.Equal(0, persistedMessage.RetryCount);
        Assert.Null(persistedMessage.LastError);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
