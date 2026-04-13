using Joby.Application.Interfaces;
using Joby.Infrastructure.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Joby.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(string to, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Smtp.Host))
        {
            _logger.LogWarning("Email skipped: Email:Smtp:Host is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            _logger.LogWarning("Email skipped: Email:FromAddress is not configured");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = textBody };

            using var client = new SmtpClient();
            var secure = _settings.Smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, secure, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Smtp.User))
            {
                await client.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> TrySendWithAttachmentsAsync(
        string to,
        string subject,
        string textBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Smtp.Host))
        {
            _logger.LogWarning("Email skipped: Email:Smtp:Host is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            _logger.LogWarning("Email skipped: Email:FromAddress is not configured");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart("plain") { Text = textBody });

            foreach (var att in attachments)
            {
                var contentType = SafeContentType(att.ContentType, att.FileName);
                var part = new MimePart(contentType.MediaType, contentType.MediaSubtype)
                {
                    Content = new MimeContent(new MemoryStream(att.Content, writable: false)),
                    FileName = att.FileName,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                };
                multipart.Add(part);
            }

            message.Body = multipart;

            using var client = new SmtpClient();
            var secure = _settings.Smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, secure, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Smtp.User))
            {
                await client.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to {To}", to);
            return false;
        }
    }

    private static ContentType SafeContentType(string? contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            try
            {
                return ContentType.Parse(contentType);
            }
            catch
            {
                // fall through
            }
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => new ContentType("application", "pdf"),
            ".doc" => new ContentType("application", "msword"),
            ".docx" => new ContentType("application", "vnd.openxmlformats-officedocument.wordprocessingml.document"),
            _ => new ContentType("application", "octet-stream"),
        };
    }
}
