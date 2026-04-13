namespace Joby.Application.Interfaces;

public interface IEmailSender
{
    /// <summary>Sends an email. Returns false if email is disabled or misconfigured.</summary>
    Task<bool> TrySendAsync(string to, string subject, string textBody, CancellationToken cancellationToken = default);

    /// <summary>Sends an email with file attachments (e.g. career applications).</summary>
    Task<bool> TrySendWithAttachmentsAsync(
        string to,
        string subject,
        string textBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default);
}
