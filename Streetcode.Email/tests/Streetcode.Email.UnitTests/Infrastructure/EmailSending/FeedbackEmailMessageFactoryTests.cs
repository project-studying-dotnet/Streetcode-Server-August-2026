using Microsoft.Extensions.Options;
using MimeKit;
using Streetcode.Email.Application.EmailTemplates;
using Streetcode.Email.Domain.EmailDeliveries;
using Streetcode.Email.Infrastructure.EmailSending;

namespace Streetcode.Email.UnitTests.Infrastructure.EmailSending;

public sealed class FeedbackEmailMessageFactoryTests
{
    private const string SenderAddress = "noreply@streetcode.com";
    private const string RecipientAddress = "feedback@streetcode.com";

    [Fact]
    public void Constructor_WithNullSmtpOptions_ThrowsArgumentNullException()
    {
        var feedbackOptions = Options.Create(CreateFeedbackOptions());

        var exception = Assert.Throws<ArgumentNullException>(
            () => new FeedbackEmailMessageFactory(
                null!,
                feedbackOptions));

        Assert.Equal("smtpOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullFeedbackOptions_ThrowsArgumentNullException()
    {
        var smtpOptions = Options.Create(CreateSmtpOptions());

        var exception = Assert.Throws<ArgumentNullException>(
            () => new FeedbackEmailMessageFactory(
                smtpOptions,
                null!));

        Assert.Equal("feedbackEmailOptions", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullDelivery_ThrowsArgumentNullException()
    {
        var factory = CreateFactory();

        var exception = Assert.Throws<ArgumentNullException>(
            () => factory.Create(null!));

        Assert.Equal("delivery", exception.ParamName);
    }

    [Fact]
    public void Create_WithValidDelivery_CreatesExpectedMimeMessage()
    {
        const string feedbackSender = "author@example.com";
        const string content = "Дякую за проєкт!";
        var factory = CreateFactory();
        var delivery = CreateDelivery(
            FeedbackEmailTemplatePolicy.TemplateName,
            new Dictionary<string, string>
            {
                [FeedbackEmailTemplatePolicy.SenderEmailKey] = feedbackSender,
                [FeedbackEmailTemplatePolicy.ContentKey] = content
            });

        using var message = factory.Create(delivery);

        var from = Assert.IsType<MailboxAddress>(Assert.Single(message.From));
        var to = Assert.IsType<MailboxAddress>(Assert.Single(message.To));
        var body = Assert.IsType<TextPart>(message.Body);

        Assert.Equal(SenderAddress, from.Address);
        Assert.Equal(RecipientAddress, to.Address);
        Assert.Equal("Streetcode feedback", message.Subject);
        Assert.True(body.IsHtml);
        Assert.Contains(feedbackSender, body.Text);
        Assert.Contains(content, body.Text);
    }

    [Fact]
    public void Create_WithUnsafeContent_EncodesHtmlAndPreservesLineBreaks()
    {
        const string unsafeContent =
            "<script>alert(\"x\")</script>\r\nSecond & line";
        var factory = CreateFactory();
        var delivery = CreateDelivery(
            FeedbackEmailTemplatePolicy.TemplateName,
            new Dictionary<string, string>
            {
                [FeedbackEmailTemplatePolicy.SenderEmailKey] =
                    "author@example.com",
                [FeedbackEmailTemplatePolicy.ContentKey] = unsafeContent
            });

        using var message = factory.Create(delivery);

        var body = Assert.IsType<TextPart>(message.Body);

        Assert.DoesNotContain("<script>", body.Text);
        Assert.Contains(
            "&lt;script&gt;alert(&quot;x&quot;)&lt;/script&gt;",
            body.Text);
        Assert.Contains("Second &amp; line", body.Text);
        Assert.Contains("<br />", body.Text);
    }

    [Fact]
    public void Create_WithUnsupportedTemplate_ThrowsInvalidOperationException()
    {
        var factory = CreateFactory();
        var delivery = CreateDelivery(
            "unsupported.v1",
            CreateValidTemplateData());

        var exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(delivery));

        Assert.Equal(
            "Unsupported email template 'unsupported.v1'.",
            exception.Message);
    }

    [Theory]
    [InlineData(FeedbackEmailTemplatePolicy.SenderEmailKey)]
    [InlineData(FeedbackEmailTemplatePolicy.ContentKey)]
    public void Create_WithoutRequiredTemplateValue_ThrowsInvalidOperationException(
        string missingKey)
    {
        var templateData = CreateValidTemplateData();
        templateData.Remove(missingKey);
        var factory = CreateFactory();
        var delivery = CreateDelivery(
            FeedbackEmailTemplatePolicy.TemplateName,
            templateData);

        var exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(delivery));

        Assert.Equal(
            $"Required template value '{missingKey}' is missing.",
            exception.Message);
    }

    private static FeedbackEmailMessageFactory CreateFactory()
    {
        return new FeedbackEmailMessageFactory(
            Options.Create(CreateSmtpOptions()),
            Options.Create(CreateFeedbackOptions()));
    }

    private static SmtpOptions CreateSmtpOptions()
    {
        return new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 465,
            UseSsl = true,
            Username = "smtp-user",
            Password = "smtp-password",
            SenderAddress = SenderAddress
        };
    }

    private static FeedbackEmailOptions CreateFeedbackOptions()
    {
        return new FeedbackEmailOptions
        {
            RecipientAddress = RecipientAddress
        };
    }

    private static EmailDelivery CreateDelivery(
        string template,
        IReadOnlyDictionary<string, string> templateData)
    {
        return new EmailDelivery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            template,
            null,
            templateData);
    }

    private static Dictionary<string, string> CreateValidTemplateData()
    {
        return new Dictionary<string, string>
        {
            [FeedbackEmailTemplatePolicy.SenderEmailKey] =
                "author@example.com",
            [FeedbackEmailTemplatePolicy.ContentKey] = "Feedback content"
        };
    }
}
