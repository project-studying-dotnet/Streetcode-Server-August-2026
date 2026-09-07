using Streetcode.Email.Application.EmailRequests;
using Streetcode.Email.Application.EmailTemplates;

namespace Streetcode.Email.UnitTests.Application.EmailRequests;

public sealed class RequestEmailDeliveryCommandValidatorTests
{
    private readonly RequestEmailDeliveryCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        var command = CreateValidCommand();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyMessageId_HasValidationError()
    {
        var command = CreateValidCommand() with { MessageId = Guid.Empty };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.MessageId));
    }

    [Fact]
    public void Validate_WithEmptyCorrelationId_HasValidationError()
    {
        var command = CreateValidCommand() with { CorrelationId = Guid.Empty };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.CorrelationId));
    }

    [Fact]
    public void Validate_WithDefaultRequestedAtUtc_HasValidationError()
    {
        var command = CreateValidCommand() with { RequestedAtUtc = default };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.RequestedAtUtc));
    }

    [Fact]
    public void Validate_WithNonUtcRequestedAtUtc_HasValidationError()
    {
        var nonUtcTimestamp = new DateTimeOffset(
            2026,
            9,
            7,
            12,
            0,
            0,
            TimeSpan.FromHours(3));
        var command = CreateValidCommand() with { RequestedAtUtc = nonUtcTimestamp };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.RequestedAtUtc));
    }

    [Fact]
    public void Validate_WithPastUtcRequestedAtUtc_HasNoRequestedAtUtcValidationError()
    {
        var pastUtcTimestamp = new DateTimeOffset(
            2020,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        var command = CreateValidCommand() with { RequestedAtUtc = pastUtcTimestamp };

        var result = validator.Validate(command);

        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.RequestedAtUtc));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTemplate_HasValidationError(string template)
    {
        var command = CreateValidCommand() with { Template = template };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.Template));
    }

    [Fact]
    public void Validate_WithTooLongTemplate_HasValidationError()
    {
        var command = CreateValidCommand() with { Template = new string('a', 101) };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.Template));
    }

    [Fact]
    public void Validate_WithUnsupportedTemplate_HasValidationError()
    {
        var command = CreateValidCommand() with { Template = "unsupported.v1" };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.Template));
    }

    [Fact]
    public void Validate_WithNullRecipient_HasNoRecipientValidationError()
    {
        var command = CreateValidCommand() with { Recipient = null };

        var result = validator.Validate(command);

        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.Recipient));
    }

    [Fact]
    public void Validate_WithRecipient_HasValidationError()
    {
        var command = CreateValidCommand() with { Recipient = "recipient@example.com" };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.Recipient));
    }

    [Fact]
    public void Validate_WithNullTemplateData_HasValidationError()
    {
        var command = CreateValidCommand() with { TemplateData = null };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.TemplateData));
    }

    [Fact]
    public void Validate_WithMissingSenderEmail_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback message",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                FeedbackEmailTemplatePolicy.SenderEmailKey);
    }

    [Fact]
    public void Validate_WithMissingContent_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] = "sender@example.com",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                FeedbackEmailTemplatePolicy.ContentKey);
    }

    [Fact]
    public void Validate_WithUnknownFeedbackKey_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] = "sender@example.com",
            [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback message",
            ["Unknown"] = "value",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.TemplateData));
    }

    [Fact]
    public void Validate_WithInvalidSenderEmail_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] = "not-an-email",
            [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback message",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                FeedbackEmailTemplatePolicy.SenderEmailKey);
    }

    [Fact]
    public void Validate_WithTooLongSenderEmail_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] =
                $"{new string('a', 70)}@example.com",
            [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback message",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                FeedbackEmailTemplatePolicy.SenderEmailKey);
    }

    [Fact]
    public void Validate_WithTooLongFeedbackContent_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] = "sender@example.com",
            [FeedbackEmailTemplatePolicy.ContentKey] = new string('a', 501),
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName ==
                $"{nameof(RequestEmailDeliveryCommand.TemplateData)}." +
                FeedbackEmailTemplatePolicy.ContentKey);
    }

    [Fact]
    public void Validate_WithTooManyTemplateDataEntries_HasValidationError()
    {
        var templateData = Enumerable.Range(1, 21)
            .ToDictionary(index => $"key-{index}", index => $"value-{index}");
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.TemplateData));
    }

    [Fact]
    public void Validate_WithTooLongTemplateDataKey_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [new string('k', 101)] = "value",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName.EndsWith(".Key", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithEmptyTemplateDataKey_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            [string.Empty] = "value",
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName.EndsWith(".Key", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithTooLongTemplateDataValue_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            ["key"] = new string('v', 10_001),
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName.EndsWith(".Value", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithEmptyTemplateDataValue_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            ["key"] = string.Empty,
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName.EndsWith(".Value", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithTooLargeTemplateData_HasValidationError()
    {
        var templateData = new Dictionary<string, string>
        {
            ["first"] = new string('a', 7_000),
            ["second"] = new string('b', 7_000),
            ["third"] = new string('c', 7_000),
        };
        var command = CreateValidCommand() with { TemplateData = templateData };

        var result = validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RequestEmailDeliveryCommand.TemplateData));
    }

    private static RequestEmailDeliveryCommand CreateValidCommand()
    {
        return new RequestEmailDeliveryCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            FeedbackEmailTemplatePolicy.TemplateName,
            null,
            new Dictionary<string, string>
            {
                [FeedbackEmailTemplatePolicy.SenderEmailKey] = "sender@example.com",
                [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback message",
            });
    }
}
