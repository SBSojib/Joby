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

        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !email.Contains('@', StringComparison.Ordinal))
            return BadRequest(new { message = "Please provide a valid email address." });

        if (resume == null || resume.Length == 0)
            return BadRequest(new { message = "Please attach your resume." });

        if (resume.Length > MaxResumeBytes)
            return BadRequest(new { message = "Resume file is too large (max 10 MB)." });

        var ext = Path.GetExtension(resume.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Resume must be a PDF or Word document (.pdf, .doc, .docx)." });

        var recipient = _configuration["Careers:RecipientEmail"]?.Trim()
            ?? "sojib.24csedu.037@gmail.com";

        await using var ms = new MemoryStream();
        await resume.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

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
}
