using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcode.Email.Application.Abstractions;
using Streetcode.Email.Application.EmailSending;
using Streetcode.Email.Domain.EmailDeliveries;

namespace Streetcode.Email.Infrastructure.EmailSending;

public sealed class MailKitEmailDeliverySender : IEmailDeliverySender
{
    private readonly FeedbackEmailMessageFactory _feedbackEmailMessageFactory;
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<MailKitEmailDeliverySender> _logger;

    public MailKitEmailDeliverySender(
        FeedbackEmailMessageFactory feedbackEmailMessageFactory,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<MailKitEmailDeliverySender> logger)
    {
        ArgumentNullException.ThrowIfNull(feedbackEmailMessageFactory);
        ArgumentNullException.ThrowIfNull(smtpOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _feedbackEmailMessageFactory = feedbackEmailMessageFactory;
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailDelivery delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var mimeMessage = _feedbackEmailMessageFactory.Create(delivery);

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _smtpOptions.Host,
                _smtpOptions.Port,
                _smtpOptions.UseSsl,
                cancellationToken);

            var username = _smtpOptions.Username;
            var password = _smtpOptions.Password;

            if (!string.IsNullOrWhiteSpace(username) &&
                !string.IsNullOrWhiteSpace(password))
            {
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                await client.AuthenticateAsync(
                    username,
                    password,
                    cancellationToken);
            }

            try
            {
                await client.SendAsync(
                    mimeMessage,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ServiceNotConnectedException)
            {
                throw;
            }
            catch (ServiceNotAuthenticatedException)
            {
                throw;
            }
            catch (SmtpCommandException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "SMTP delivery outcome is unknown for message " +
                    "{MessageId}.",
                    delivery.MessageId);

                throw new EmailDeliveryOutcomeUnknownException(
                    "SMTP delivery outcome is unknown.",
                    exception);
            }
        }
        finally
        {
            await DisconnectSafelyAsync(client);
        }
    }

    private async Task DisconnectSafelyAsync(SmtpClient client)
    {
        if (!client.IsConnected)
        {
            return;
        }

        try
        {
            await client.DisconnectAsync(
                true,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to gracefully disconnect from SMTP server.");
        }
    }
}
