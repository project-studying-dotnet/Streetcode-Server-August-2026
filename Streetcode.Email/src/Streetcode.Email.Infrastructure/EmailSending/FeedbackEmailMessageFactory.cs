using System.Net;
using Microsoft.Extensions.Options;
using MimeKit;
using Streetcode.Email.Application.EmailTemplates;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Infrastructure.EmailSending;

public sealed class FeedbackEmailMessageFactory
{
    private const string Subject = "Streetcode feedback";

    private readonly SmtpOptions _smtpOptions;
    private readonly FeedbackEmailOptions _feedbackEmailOptions;

    public FeedbackEmailMessageFactory(
        IOptions<SmtpOptions> smtpOptions,
        IOptions<FeedbackEmailOptions> feedbackEmailOptions)
    {
        ArgumentNullException.ThrowIfNull(smtpOptions);
        ArgumentNullException.ThrowIfNull(feedbackEmailOptions);

        _smtpOptions = smtpOptions.Value;
        _feedbackEmailOptions = feedbackEmailOptions.Value;
    }

    public MimeMessage Create(EmailDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        if (!string.Equals(
                delivery.Template,
                FeedbackEmailTemplatePolicy.TemplateName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported email template '{delivery.Template}'.");
        }

        var senderEmail = GetRequiredTemplateValue(
            delivery,
            FeedbackEmailTemplatePolicy.SenderEmailKey);

        var content = GetRequiredTemplateValue(
            delivery,
            FeedbackEmailTemplatePolicy.ContentKey);

        var encodedSenderEmail = WebUtility.HtmlEncode(senderEmail);
        var encodedContent = WebUtility.HtmlEncode(content)
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal);

        var message = new MimeMessage();

        message.From.Add(
            MailboxAddress.Parse(_smtpOptions.SenderAddress));

        message.To.Add(
            MailboxAddress.Parse(
                _feedbackEmailOptions.RecipientAddress));

        message.Subject = Subject;

        message.Body = new TextPart("html")
        {
            Text =
                "<h2>Новий відгук</h2>" +
                $"<p><strong>Від:</strong> {encodedSenderEmail}</p>" +
                $"<p><strong>Текст:</strong><br />{encodedContent}</p>"
        };

        return message;
    }

    private static string GetRequiredTemplateValue(
        EmailDelivery delivery,
        string key)
    {
        if (!delivery.TemplateData.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required template value '{key}' is missing.");
        }

        return value;
    }
}
