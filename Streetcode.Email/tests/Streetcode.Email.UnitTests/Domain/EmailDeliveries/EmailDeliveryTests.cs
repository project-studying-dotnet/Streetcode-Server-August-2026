using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.UnitTests.Domain.EmailDeliveries;

public sealed class EmailDeliveryTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesPendingDelivery()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var requestedAtUtc = DateTimeOffset.UtcNow;
        var templateData = CreateTemplateData();

        var delivery = new EmailDelivery(
            messageId,
            correlationId,
            requestedAtUtc,
            "feedback.v1",
            null,
            templateData);

        Assert.Equal(messageId, delivery.MessageId);
        Assert.Equal(correlationId, delivery.CorrelationId);
        Assert.Equal(requestedAtUtc, delivery.RequestedAtUtc);
        Assert.Equal("feedback.v1", delivery.Template);
        Assert.Null(delivery.Recipient);
        Assert.Equal(templateData, delivery.TemplateData);
        Assert.Equal(EmailDeliveryStatus.Pending, delivery.Status);
    }

    [Fact]
    public void Constructor_WithEmptyMessageId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateDelivery(messageId: Guid.Empty));

        Assert.Equal("messageId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyCorrelationId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateDelivery(correlationId: Guid.Empty));

        Assert.Equal("correlationId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithDefaultRequestedAtUtc_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateDelivery(requestedAtUtc: default));

        Assert.Equal("requestedAtUtc", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNonUtcRequestedAtUtc_ThrowsArgumentException()
    {
        var requestedAtUtc = new DateTimeOffset(
            2026,
            9,
            7,
            12,
            0,
            0,
            TimeSpan.FromHours(3));

        var exception = Assert.Throws<ArgumentException>(() =>
            CreateDelivery(requestedAtUtc: requestedAtUtc));

        Assert.Equal("requestedAtUtc", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTemplate_ThrowsArgumentException(
        string? template)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateDelivery(template: template!));

        Assert.Equal("template", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTemplateData_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new EmailDelivery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "feedback.v1",
                null,
                null!));

        Assert.Equal("templateData", exception.ParamName);
    }

    [Fact]
    public void Constructor_CopiesTemplateData()
    {
        var templateData = CreateTemplateData();
        var delivery = CreateDelivery(templateData: templateData);

        templateData["Content"] = "Changed content";
        templateData["Unexpected"] = "Unexpected value";

        Assert.Equal("Feedback message", delivery.TemplateData["Content"]);
        Assert.False(delivery.TemplateData.ContainsKey("Unexpected"));
    }

    [Fact]
    public void MarkAsSent_WhenStatusIsPending_ChangesStatusToSent()
    {
        var delivery = CreateDelivery();

        delivery.MarkAsSent();

        Assert.Equal(EmailDeliveryStatus.Sent, delivery.Status);
    }

    [Fact]
    public void MarkAsSent_WhenStatusIsAlreadySent_ThrowsInvalidOperationException()
    {
        var delivery = CreateDelivery();
        delivery.MarkAsSent();

        Assert.Throws<InvalidOperationException>(() => delivery.MarkAsSent());
    }

    [Fact]
    public void MarkAsFailed_WhenStatusIsPending_ChangesStatusToFailed()
    {
        var delivery = CreateDelivery();

        delivery.MarkAsFailed();

        Assert.Equal(EmailDeliveryStatus.Failed, delivery.Status);
    }

    [Fact]
    public void MarkAsFailed_WhenStatusIsSent_ThrowsInvalidOperationException()
    {
        var delivery = CreateDelivery();
        delivery.MarkAsSent();

        Assert.Throws<InvalidOperationException>(() => delivery.MarkAsFailed());
    }

    private static EmailDelivery CreateDelivery(
        Guid? messageId = null,
        Guid? correlationId = null,
        DateTimeOffset? requestedAtUtc = null,
        string template = "feedback.v1",
        string? recipient = null,
        IReadOnlyDictionary<string, string>? templateData = null)
    {
        return new EmailDelivery(
            messageId ?? Guid.NewGuid(),
            correlationId ?? Guid.NewGuid(),
            requestedAtUtc ?? DateTimeOffset.UtcNow,
            template,
            recipient,
            templateData ?? CreateTemplateData());
    }

    private static Dictionary<string, string> CreateTemplateData()
    {
        return new Dictionary<string, string>
        {
            ["From"] = "sender@example.com",
            ["Content"] = "Feedback message",
        };
    }
}
