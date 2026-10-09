using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TodoApi.Application.Abstractions.Email;

namespace TodoApi.Infrastructure.Services.Email;

public sealed class MailKitEmailSender : IEmailSender
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);

    private readonly SmtpSettings _smtpSettings;

    public MailKitEmailSender(SmtpSettings smtpSettings)
    {
        _smtpSettings = smtpSettings ?? throw new ArgumentNullException(nameof(smtpSettings));
    }

    public bool IsConfigured
    {
        get
        {
            return _smtpSettings.IsConfigured;
        }
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (_smtpSettings.IsConfigured == false)
        {
            throw new InvalidOperationException("SMTP settings are not configured.");
        }

        MimeMessage mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mimeMessage.Subject = message.Subject;

        BodyBuilder bodyBuilder = new BodyBuilder
        {
            TextBody = message.PlainTextBody,
            HtmlBody = message.HtmlBody
        };
        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using SmtpClient smtpClient = new SmtpClient();
        smtpClient.Timeout = (int)SendTimeout.TotalMilliseconds;

        try
        {
            await smtpClient.ConnectAsync(
                _smtpSettings.Host,
                _smtpSettings.Port,
                ToSecureSocketOptions(_smtpSettings.Security),
                cancellationToken);

            if (_smtpSettings.Username != null && _smtpSettings.Password != null)
            {
                await smtpClient.AuthenticateAsync(
                    _smtpSettings.Username,
                    _smtpSettings.Password,
                    cancellationToken);
            }

            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (SmtpCommandException exception) when (IsRejectionOfThisMessage(exception))
        {
            throw new EmailRejectedException(
                $"The mail server rejected the message ({(int)exception.StatusCode}).",
                exception);
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested == false)
        {
            throw new EmailServiceUnavailableException(
                "The mail server could not be reached or refused the session.",
                exception);
        }
    }

    private static bool IsRejectionOfThisMessage(SmtpCommandException exception)
    {
        return exception.ErrorCode == SmtpErrorCode.RecipientNotAccepted
            || exception.ErrorCode == SmtpErrorCode.MessageNotAccepted;
    }

    private static SecureSocketOptions ToSecureSocketOptions(SmtpSecurity security)
    {
        if (security == SmtpSecurity.SslOnConnect)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (security == SmtpSecurity.None)
        {
            return SecureSocketOptions.None;
        }

        return SecureSocketOptions.StartTls;
    }
}
