using Joby.Application.DTOs.Applications;
using Joby.Application.Interfaces;
using Joby.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var userId = GetUserId();
        var application = await _applicationService.CreateApplicationAsync(userId, request);
        return CreatedAtAction(nameof(GetApplication), new { applicationId = application.Id }, application);
    }

    [HttpGet("{applicationId}")]
    public async Task<ActionResult<ApplicationDto>> GetApplication(Guid applicationId)
    {
        var userId = GetUserId();
        var application = await _applicationService.GetApplicationAsync(userId, applicationId);
        if (application == null)
        {
            return NotFound();
        }
        return Ok(application);
    }

    [HttpGet]
    public async Task<ActionResult<List<ApplicationDto>>> GetApplications()
    {
        var userId = GetUserId();
        var applications = await _applicationService.GetApplicationsAsync(userId);
        return Ok(applications);
    }

    [HttpGet("pipeline")]
    public async Task<ActionResult<Dictionary<ApplicationStatus, List<ApplicationDto>>>> GetPipeline()
    {
        var userId = GetUserId();
        var pipeline = await _applicationService.GetApplicationsPipelineAsync(userId);
        return Ok(pipeline);
    }

    [HttpPut("{applicationId}/status")]
    public async Task<ActionResult<ApplicationDto>> UpdateStatus(
        Guid applicationId,
        [FromBody] UpdateApplicationStatusRequest request)
    {
        var userId = GetUserId();
        var application = await _applicationService.UpdateStatusAsync(userId, applicationId, request);
        return Ok(application);
    }

    [HttpPut("{applicationId}")]
    public async Task<ActionResult<ApplicationDto>> UpdateApplication(
        Guid applicationId,
        [FromBody] UpdateApplicationRequest request)
    {
        var userId = GetUserId();
        var application = await _applicationService.UpdateApplicationAsync(userId, applicationId, request);
        return Ok(application);
    }

    [HttpPost("{applicationId}/events")]
    public async Task<ActionResult<ApplicationEventDto>> AddEvent(
        Guid applicationId,
        [FromBody] AddApplicationEventRequest request)
    {
        var userId = GetUserId();
        var appEvent = await _applicationService.AddEventAsync(userId, applicationId, request);
        return Ok(appEvent);
    }

    [HttpDelete("{applicationId}")]
    public async Task<IActionResult> DeleteApplication(Guid applicationId)
    {
        var userId = GetUserId();
        await _applicationService.DeleteApplicationAsync(userId, applicationId);
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


