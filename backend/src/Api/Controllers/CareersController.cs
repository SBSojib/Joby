using System.Net.Mail;
using System.Text;
using Joby.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CareersController : ControllerBase
{
    private const long MaxResumeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx",
    };

    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CareersController> _logger;

    public CareersController(
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<CareersController> logger)
    {
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Public endpoint: submits a career application with resume attachment via email.</summary>
    [HttpPost("applications")]
    [AllowAnonymous]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxResumeBytes)]
    [RequestSizeLimit(MaxResumeBytes)]
    public async Task<IActionResult> SubmitApplication(
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string? roleInterest,
        [FromForm] string? message,
        [FromForm] IFormFile? resume,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 200)
            return BadRequest(new { message = "Please provide your full name (max 200 characters)." });

        if (!IsValidEmail(email))
            return BadRequest(new { message = "Please provide a valid email address." });

        if (resume == null || resume.Length == 0)
            return BadRequest(new { message = "Please attach your resume." });

        if (resume.Length > MaxResumeBytes)
            return BadRequest(new { message = "Resume file is too large (max 10 MB)." });

        var ext = Path.GetExtension(resume.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Resume must be a PDF or Word document (.pdf, .doc, .docx)." });

        var recipient = _configuration["Careers:RecipientEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogError("Career application recipient is not configured");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "We could not deliver your application right now. Please try again later." });
        }

        await using var ms = new MemoryStream();
        await resume.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        if (!IsSupportedResumeSignature(bytes, ext))
            return BadRequest(new { message = "Resume contents do not match a supported document format." });

        var body = new StringBuilder()
            .AppendLine("New career application (Joby)")
            .AppendLine()
            .AppendLine($"Name: {fullName}")
            .AppendLine($"Email: {email}")
            .AppendLine($"Role interest: {roleInterest ?? "(not specified)"}")
            .AppendLine()
            .AppendLine("Message:")
            .AppendLine(message ?? "(none)")
            .ToString();

        var attachment = new EmailAttachment(
            string.IsNullOrWhiteSpace(resume.FileName) ? "resume" + ext : resume.FileName,
            bytes,
            resume.ContentType);

        var sent = await _emailSender.TrySendWithAttachmentsAsync(
            recipient,
            $"Joby careers: application from {fullName}",
            body,
            new[] { attachment },
            cancellationToken);

        if (!sent)
        {
            _logger.LogWarning("Career application email was not sent (SMTP may be unconfigured). Applicant: {Email}", email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "We could not deliver your application right now. Please try again later or email your resume directly." });
        }

        return Accepted(new { message = "Thank you — your application was submitted." });
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
            return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSupportedResumeSignature(byte[] bytes, string extension)
    {
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return bytes.Length >= 4
                   && bytes[0] == 0x25
                   && bytes[1] == 0x50
                   && bytes[2] == 0x44
                   && bytes[3] == 0x46;
        }

        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return bytes.Length >= 4
                   && bytes[0] == 0x50
                   && bytes[1] == 0x4b
                   && bytes[2] == 0x03
                   && bytes[3] == 0x04;
        }

        return string.Equals(extension, ".doc", StringComparison.OrdinalIgnoreCase)
               && bytes.Length >= 8
               && bytes[0] == 0xd0
               && bytes[1] == 0xcf
               && bytes[2] == 0x11
               && bytes[3] == 0xe0
               && bytes[4] == 0xa1
               && bytes[5] == 0xb1
               && bytes[6] == 0x1a
               && bytes[7] == 0xe1;
    }
}
