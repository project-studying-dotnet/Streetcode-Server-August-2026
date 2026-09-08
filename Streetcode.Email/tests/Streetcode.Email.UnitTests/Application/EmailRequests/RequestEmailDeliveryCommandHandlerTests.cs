using FluentValidation;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Application.EmailRequests;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.UnitTests.Application.EmailRequests;

public sealed class RequestEmailDeliveryCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNewDelivery_SavesBeforeEnqueueing()
    {
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);
        var command = CreateCommand();

        await handler.HandleAsync(command, CancellationToken.None);

        var addedDelivery = Assert.IsType<EmailDelivery>(
            repository.AddedDelivery);

        Assert.Equal(command.MessageId, addedDelivery.MessageId);
        Assert.Equal(command.CorrelationId, addedDelivery.CorrelationId);
        Assert.Equal(command.RequestedAtUtc, addedDelivery.RequestedAtUtc);
        Assert.Equal(command.Template, addedDelivery.Template);
        Assert.Equal(command.Recipient, addedDelivery.Recipient);
        Assert.Equal(command.TemplateData, addedDelivery.TemplateData);
        Assert.Equal(EmailDeliveryStatus.Pending, addedDelivery.Status);
        Assert.Equal(
            new[] { "Get", "Add", "Save", "Enqueue" },
            calls);
        Assert.Equal(
            new[] { command.MessageId },
            scheduler.EnqueuedMessageIds);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingPendingDelivery_EnqueuesAgain()
    {
        var command = CreateCommand();
        var existingDelivery = CreateDelivery(
            command,
            new Dictionary<string, string>
            {
                ["Content"] = "Feedback message",
                ["From"] = "sender@example.com",
            });
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            existingDelivery);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Null(repository.AddedDelivery);
        Assert.Equal(new[] { "Get", "Enqueue" }, calls);
        Assert.Equal(
            new[] { command.MessageId },
            scheduler.EnqueuedMessageIds);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingSentDelivery_DoesNotEnqueue()
    {
        var command = CreateCommand();
        var existingDelivery = CreateDelivery(command);
        existingDelivery.MarkAsSent();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            existingDelivery);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Null(repository.AddedDelivery);
        Assert.Empty(scheduler.EnqueuedMessageIds);
        Assert.Equal(new[] { "Get" }, calls);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingFailedDelivery_DoesNotEnqueue()
    {
        var command = CreateCommand();
        var existingDelivery = CreateDelivery(command);
        existingDelivery.MarkAsFailed();
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            existingDelivery);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Null(repository.AddedDelivery);
        Assert.Empty(scheduler.EnqueuedMessageIds);
        Assert.Equal(new[] { "Get" }, calls);
    }

    [Fact]
    public async Task HandleAsync_WithSameMessageIdAndDifferentData_Throws()
    {
        var command = CreateCommand();
        var differentTemplateData = new Dictionary<string, string>
        {
            ["From"] = "sender@example.com",
            ["Content"] = "Different feedback message",
        };
        var existingDelivery = CreateDelivery(
            command,
            differentTemplateData);
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(
            calls,
            existingDelivery);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal(
            "The same MessageId was received with different request data.",
            exception.Message);
        Assert.Null(repository.AddedDelivery);
        Assert.Empty(scheduler.EnqueuedMessageIds);
        Assert.Equal(new[] { "Get" }, calls);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_StopsBeforeRepository()
    {
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var scheduler = new FakeEmailJobScheduler(calls);
        var validator = new InlineValidator<RequestEmailDeliveryCommand>();
        validator.RuleFor(command => command.MessageId).NotEmpty();
        var handler = CreateHandler(repository, scheduler, validator);
        var command = CreateCommand() with { MessageId = Guid.Empty };

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Empty(calls);
        Assert.Null(repository.AddedDelivery);
        Assert.Empty(scheduler.EnqueuedMessageIds);
    }

    [Fact]
    public async Task HandleAsync_WhenEnqueueFails_PropagatesExceptionAfterSave()
    {
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var expectedException = new InvalidOperationException(
            "Hangfire storage is unavailable.");
        var scheduler = new FakeEmailJobScheduler(
            calls,
            expectedException);
        var handler = CreateHandler(repository, scheduler);
        var command = CreateCommand();

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal(
            new[] { "Get", "Add", "Save", "Enqueue" },
            calls);
        Assert.Equal(
            EmailDeliveryStatus.Pending,
            repository.AddedDelivery?.Status);
    }

    [Fact]
    public async Task HandleAsync_WithNullCommand_ThrowsBeforeDependencies()
    {
        var calls = new List<string>();
        var repository = new FakeEmailDeliveryRepository(calls);
        var scheduler = new FakeEmailJobScheduler(calls);
        var handler = CreateHandler(repository, scheduler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        Assert.Empty(calls);
    }

    private static RequestEmailDeliveryCommandHandler CreateHandler(
        FakeEmailDeliveryRepository repository,
        FakeEmailJobScheduler scheduler,
        IValidator<RequestEmailDeliveryCommand>? validator = null)
    {
        return new RequestEmailDeliveryCommandHandler(
            validator ?? new InlineValidator<RequestEmailDeliveryCommand>(),
            repository,
            scheduler);
    }

    private static RequestEmailDeliveryCommand CreateCommand()
    {
        return new RequestEmailDeliveryCommand(
            Guid.Parse("5e9bb4a4-321b-4863-b0f7-873db894695c"),
            Guid.Parse("62cbab4b-8102-4779-a044-2ab783057d15"),
            new DateTimeOffset(
                2026,
                9,
                8,
                9,
                0,
                0,
                TimeSpan.Zero),
            "feedback.v1",
            null,
            new Dictionary<string, string>
            {
                ["From"] = "sender@example.com",
                ["Content"] = "Feedback message",
            });
    }

    private static EmailDelivery CreateDelivery(
        RequestEmailDeliveryCommand command,
        IReadOnlyDictionary<string, string>? templateData = null)
    {
        return new EmailDelivery(
            command.MessageId,
            command.CorrelationId,
            command.RequestedAtUtc,
            command.Template,
            command.Recipient,
            templateData ?? command.TemplateData!);
    }

    private sealed class FakeEmailDeliveryRepository
        : IEmailDeliveryRepository
    {
        private readonly List<string> calls;
        private readonly EmailDelivery? existingDelivery;

        public FakeEmailDeliveryRepository(
            List<string> calls,
            EmailDelivery? existingDelivery = null)
        {
            this.calls = calls;
            this.existingDelivery = existingDelivery;
        }

        public EmailDelivery? AddedDelivery { get; private set; }

        public Task<EmailDelivery?> GetByMessageIdAsync(
            Guid messageId,
            CancellationToken cancellationToken)
        {
            calls.Add("Get");

            return Task.FromResult(existingDelivery);
        }

        public Task AddAsync(
            EmailDelivery emailDelivery,
            CancellationToken cancellationToken)
        {
            calls.Add("Add");
            AddedDelivery = emailDelivery;

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            calls.Add("Save");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailJobScheduler : IEmailJobScheduler
    {
        private readonly List<string> calls;
        private readonly Exception? exceptionToThrow;

        public FakeEmailJobScheduler(
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            this.calls = calls;
            this.exceptionToThrow = exceptionToThrow;
        }

        public List<Guid> EnqueuedMessageIds { get; } = new();

        public Task EnqueueAsync(
            Guid messageId,
            CancellationToken cancellationToken)
        {
            calls.Add("Enqueue");
            EnqueuedMessageIds.Add(messageId);

            return exceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(exceptionToThrow);
        }
    }
}
