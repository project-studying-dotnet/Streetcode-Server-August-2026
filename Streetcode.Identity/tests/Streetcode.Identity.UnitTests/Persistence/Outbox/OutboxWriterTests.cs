using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application.IntegrationEvents;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

namespace Streetcode.Identity.UnitTests.Persistence.Outbox;

public class OutboxWriterTests
{
    [Fact]
    public async Task AddAsync_WhenEventIsPassedThroughInterface_ShouldStoreConcreteEventTypeAndPayload()
    {
        await using var context = CreateContext();
        var writer = new OutboxWriter(context);

        var eventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var occurredAt = new DateTimeOffset(
            2026, 8, 24, 10, 30, 0, TimeSpan.Zero);

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

        var entry = Assert.Single(context.ChangeTracker.Entries<OutboxMessage>());

        Assert.Equal(EntityState.Added, entry.State);

        var message = entry.Entity;
        Assert.Equal(eventId, message.Id);
        Assert.Equal("UserAccessChangedV1", message.Type);
        Assert.Equal(userId.ToString(), message.Key);
        Assert.Equal(occurredAt, message.OccurredAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.LastError);

        using var document = JsonDocument.Parse(message.Payload);
        var root = document.RootElement;

        Assert.Equal(eventId, root.GetProperty("eventId").GetGuid());
        Assert.Equal(userId, root.GetProperty("userId").GetGuid());
        Assert.False(root.GetProperty("isActive").GetBoolean());
        Assert.Equal(2, root.GetProperty("accessVersion").GetInt64());
        Assert.Equal(
            occurredAt,
            root.GetProperty("occurredAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task AddAsync_WhenEventIsNull_ShouldThrowArgumentNullExceptionAndNotTrackMessage()
    {
        await using var context = CreateContext();
        var writer = new OutboxWriter(context);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            writer.AddAsync<IIntegrationEvent>(
                null!,
                "11111111-1111-1111-1111-111111111111",
                CancellationToken.None));

        Assert.Equal("integrationEvent", exception.ParamName);
        Assert.Empty(context.ChangeTracker.Entries<OutboxMessage>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_WhenKeyIsEmptyOrWhitespace_ShouldThrowArgumentExceptionAndNotTrackMessage(
        string key)
    {
        await using var context = CreateContext();
        var writer = new OutboxWriter(context);
        IIntegrationEvent integrationEvent = new UserAccessChangedV1(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            false,
            2,
            new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            writer.AddAsync(
                integrationEvent,
                key,
                CancellationToken.None));

        Assert.Equal("key", exception.ParamName);
        Assert.Empty(context.ChangeTracker.Entries<OutboxMessage>());
    }

    private static StreetcodeIdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer("Server=.;Database=OutboxWriterTests;")
            .Options;

        return new StreetcodeIdentityDbContext(options);
    }
}
