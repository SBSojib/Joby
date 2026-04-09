using Joby.Application.DTOs.Profile;
using Joby.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var profile = await _profileService.UpdateProfileAsync(userId, request);
        return Ok(profile);
    }

    [HttpPost("resumes")]
    public async Task<ActionResult<ResumeDto>> UploadResume(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required" });
        }

        var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Only PDF and DOCX files are allowed" });
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            return BadRequest(new { message = "File size must be less than 10MB" });
        }

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var resume = await _profileService.UploadResumeAsync(userId, stream, file.FileName, file.ContentType);
        return Ok(resume);
    }

    [HttpGet("resumes")]
    public async Task<ActionResult<List<ResumeDto>>> GetResumes()
    {
        var userId = GetUserId();
        var resumes = await _profileService.GetResumesAsync(userId);
        return Ok(resumes);
    }

    [HttpGet("resumes/{resumeId}")]
    public async Task<ActionResult<ResumeDto>> GetResume(Guid resumeId)
    {
        var userId = GetUserId();
        var resume = await _profileService.GetResumeAsync(userId, resumeId);
        if (resume == null)
        {
            return NotFound();
        }
        return Ok(resume);
    }

    [HttpGet("resumes/{resumeId}/download")]
    public async Task<IActionResult> DownloadResume(Guid resumeId)
    {
        var userId = GetUserId();
        var resume = await _profileService.GetResumeAsync(userId, resumeId);
        if (resume == null)
        {
            return NotFound();
        }

        var fileData = await _profileService.GetResumeFileAsync(userId, resumeId);
        if (fileData == null)
        {
            return NotFound();
        }

        return File(fileData, resume.ContentType, resume.FileName);
    }

    [HttpPut("resumes/{resumeId}/active")]
    public async Task<IActionResult> SetActiveResume(Guid resumeId)
    {
        var userId = GetUserId();
        await _profileService.SetActiveResumeAsync(userId, resumeId);
        return Ok(new { message = "Active resume updated" });
    }

    [HttpDelete("resumes/{resumeId}")]
    public async Task<IActionResult> DeleteResume(Guid resumeId)
    {
        var userId = GetUserId();
        await _profileService.DeleteResumeAsync(userId, resumeId);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}




