using Joby.Application.DTOs.Common;
using Joby.Application.DTOs.Jobs;
using Joby.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IRecommendationService _recommendationService;

    public JobsController(IJobService jobService, IRecommendationService recommendationService)
    {
        _jobService = jobService;
        _recommendationService = recommendationService;
    }

    [HttpPost]
    public async Task<ActionResult<JobDto>> CreateJob([FromBody] CreateJobRequest request)
    {
        var userId = GetUserId();
        var job = await _jobService.CreateJobAsync(userId, request);
        return CreatedAtAction(nameof(GetJob), new { jobId = job.Id }, job);
    }

    [HttpPost("url")]
    public async Task<ActionResult<JobDto>> CreateJobByUrl([FromBody] CreateJobByUrlRequest request)
    {
        var userId = GetUserId();
        var job = await _jobService.CreateJobByUrlAsync(userId, request.Url);
        return CreatedAtAction(nameof(GetJob), new { jobId = job.Id }, job);
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<JobDto>> GetJob(Guid jobId)
    {
        var userId = GetUserId();
        var job = await _jobService.GetJobAsync(userId, jobId);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpGet("recommended")]
    public async Task<ActionResult<PagedResult<JobWithRecommendationDto>>> GetRecommendedJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var jobs = await _jobService.GetRecommendedJobsAsync(userId, page, pageSize);
        return Ok(jobs);
    }

    [HttpGet("top-recommendations")]
    public async Task<ActionResult<List<JobWithRecommendationDto>>> GetTopRecommendations(
        [FromQuery] int count = 10)
    {
        var userId = GetUserId();
        var jobs = await _recommendationService.GetTopRecommendationsAsync(userId, count);
        return Ok(jobs);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<JobDto>>> SearchJobs([FromQuery] JobSearchRequest request)
    {
        var userId = GetUserId();
        var jobs = await _jobService.SearchJobsAsync(userId, request);
        return Ok(jobs);
    }

    [HttpPut("{jobId}")]
    public async Task<ActionResult<JobDto>> UpdateJob(Guid jobId, [FromBody] CreateJobRequest request)
    {
        var userId = GetUserId();
        var job = await _jobService.UpdateJobAsync(userId, jobId, request);
        return Ok(job);
    }

    [HttpDelete("{jobId}")]
    public async Task<IActionResult> DeleteJob(Guid jobId)
    {
        var userId = GetUserId();
        await _jobService.DeleteJobAsync(userId, jobId);
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




