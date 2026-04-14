using System.Text.Json;
using Joby.Application.DTOs.Applications;
using Joby.Application.DTOs.Common;
using Joby.Application.DTOs.Jobs;
using Joby.Application.Interfaces;
using Joby.Domain.Entities;
using Joby.Domain.Enums;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Joby.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;
    private readonly IJobScraper _jobScraper;
    private readonly IRecommendationService _recommendationService;
    private readonly IApplicationService _applicationService;
    private readonly IReminderService _reminderService;

    public JobService(
        ApplicationDbContext context,
        IJobScraper jobScraper,
        IRecommendationService recommendationService,
        IApplicationService applicationService,
        IReminderService reminderService)
    {
        _context = context;
        _jobScraper = jobScraper;
        _recommendationService = recommendationService;
        _applicationService = applicationService;
        _reminderService = reminderService;
    }

    public async Task<JobDto> CreateJobAsync(Guid userId, CreateJobRequest request)
    {
        var job = new Job
        {
            UserId = userId,
            Title = request.Title,
            Company = request.Company,
            Location = request.Location,
            Description = request.Description,
            Requirements = request.Requirements,
            Salary = request.Salary,
            JobType = request.JobType,
            SourceUrl = request.SourceUrl,
            SourcePlatform = request.SourcePlatform,
            PostedDate = request.PostedDate,
            ExpiryDate = request.ExpiryDate,
            IsExtracted = false
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        await EnsureSavedApplicationForJobAsync(userId, job.Id);

        // Trigger recommendation computation
        _ = Task.Run(async () => await _recommendationService.ComputeRecommendationsForJobAsync(job.Id));

        return await MapToDto(job);
    }

    public async Task<JobDto> CreateJobByUrlAsync(Guid userId, string url)
    {
        // Check if job with same URL already exists for this user
        var existingJob = await _context.Jobs
            .FirstOrDefaultAsync(j => j.UserId == userId && j.SourceUrl == url);

        if (existingJob != null)
        {
            await ApplyScrapeToJobAsync(existingJob, url);
            await EnsureSavedApplicationForJobAsync(userId, existingJob.Id);
            return await MapToDto(existingJob, includeSourcePageHtml: true);
        }

        var scrape = await _jobScraper.ScrapeJobAsync(url);
        var meta = scrape?.Metadata;

        var job = new Job
        {
            UserId = userId,
            Title = meta?.Title ?? "Unknown Position",
            Company = meta?.Company ?? "Unknown Company",
            Location = meta?.Location,
            // Listing body is shown from saved HTML; avoid storing JSON-LD / scraped fragments as plain text.
            Description = string.IsNullOrEmpty(scrape?.RawPageHtml) ? meta?.Description : null,
            Requirements = meta?.Requirements,
            Salary = meta?.Salary,
            JobType = meta?.JobType,
            SourceUrl = url,
            SourcePageHtml = scrape?.RawPageHtml,
            SourcePlatform = DetectPlatform(url),
            PostedDate = meta?.PostedDate,
            IsExtracted = scrape != null,
            ExtractedDataJson = meta != null ? JsonSerializer.Serialize(meta) : null
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        await EnsureSavedApplicationForJobAsync(userId, job.Id);

        // Trigger recommendation computation
        _ = Task.Run(async () => await _recommendationService.ComputeRecommendationsForJobAsync(job.Id));

        return await MapToDto(job, includeSourcePageHtml: true);
    }

    /// <summary>
    /// Re-fetch listing HTML and metadata when the same URL is added again (fixes stale rows and duplicate URL flows).
    /// </summary>
    private async Task ApplyScrapeToJobAsync(Job job, string url)
    {
        var scrape = await _jobScraper.ScrapeJobAsync(url);
        if (scrape == null)
        {
            return;
        }

        var meta = scrape.Metadata;

        if (!string.IsNullOrWhiteSpace(meta.Title))
        {
            job.Title = meta.Title;
        }

        if (!string.IsNullOrWhiteSpace(meta.Company))
        {
            job.Company = meta.Company;
        }

        if (!string.IsNullOrWhiteSpace(meta.Location))
        {
            job.Location = meta.Location;
        }

        if (!string.IsNullOrWhiteSpace(meta.Requirements))
        {
            job.Requirements = meta.Requirements;
        }

        if (!string.IsNullOrWhiteSpace(meta.Salary))
        {
            job.Salary = meta.Salary;
        }

        if (!string.IsNullOrWhiteSpace(meta.JobType))
        {
            job.JobType = meta.JobType;
        }

        if (meta.PostedDate.HasValue)
        {
            job.PostedDate = meta.PostedDate;
        }

        job.ExtractedDataJson = JsonSerializer.Serialize(meta);

        if (!string.IsNullOrEmpty(scrape.RawPageHtml))
        {
            job.SourcePageHtml = scrape.RawPageHtml;
            job.Description = null;
        }
        else if (!string.IsNullOrWhiteSpace(meta.Description))
        {
            job.Description = meta.Description;
        }

        job.SourceUrl = url;
        job.SourcePlatform = DetectPlatform(url);
        job.IsExtracted = scrape != null;
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task EnsureSavedApplicationForJobAsync(Guid userId, Guid jobId)
    {
        var exists = await _context.Applications
            .AnyAsync(a => a.JobId == jobId && a.UserId == userId);
        if (exists)
        {
            return;
        }

        await _applicationService.CreateApplicationAsync(userId, new CreateApplicationRequest
        {
            JobId = jobId,
            Status = ApplicationStatus.Saved
        });
    }

    public async Task<JobDto?> GetJobAsync(Guid userId, Guid jobId)
    {
        var job = await _context.Jobs
            .Include(j => j.Application)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        if (job == null)
        {
            return null;
        }

        var reminders = await _reminderService.GetRemindersForJobAsync(userId, jobId);
        return await MapToDto(job, includeSourcePageHtml: true, reminders: reminders);
    }

    public async Task<PagedResult<JobWithRecommendationDto>> GetRecommendedJobsAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Recommendations
            .Include(r => r.Job)
                .ThenInclude(j => j.Application)
            .Where(r => r.UserId == userId && r.IsActive && r.Job.UserId == userId)
            .OrderByDescending(r => r.Score);

        var totalCount = await query.CountAsync();

        var recommendations = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = recommendations.Select(r => new JobWithRecommendationDto
        {
            Id = r.Job.Id,
            Title = r.Job.Title,
            Company = r.Job.Company,
            Location = r.Job.Location,
            Description = r.Job.Description,
            Requirements = r.Job.Requirements,
            Salary = r.Job.Salary,
            JobType = r.Job.JobType,
            SourceUrl = r.Job.SourceUrl,
            SourcePlatform = r.Job.SourcePlatform,
            PostedDate = r.Job.PostedDate,
            ExpiryDate = r.Job.ExpiryDate,
            IsExtracted = r.Job.IsExtracted,
            HasApplication = r.Job.Application != null,
            ApplicationId = r.Job.Application?.Id,
            CreatedAt = r.Job.CreatedAt,
            UpdatedAt = r.Job.UpdatedAt,
            RecommendationScore = r.Score,
            MatchedSkills = JsonSerializer.Deserialize<List<string>>(r.MatchedSkillsJson) ?? new(),
            MissingSkills = JsonSerializer.Deserialize<List<string>>(r.MissingSkillsJson) ?? new(),
            MatchedKeywords = JsonSerializer.Deserialize<List<string>>(r.MatchedKeywordsJson) ?? new(),
            MissingKeywords = JsonSerializer.Deserialize<List<string>>(r.MissingKeywordsJson) ?? new()
        }).ToList();

        return new PagedResult<JobWithRecommendationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<JobDto>> SearchJobsAsync(Guid userId, JobSearchRequest request)
    {
        var query = _context.Jobs
            .Include(j => j.Application)
            .Where(j => j.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(searchTerm) ||
                j.Company.ToLower().Contains(searchTerm) ||
                (j.Description != null && j.Description.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(j => j.Location != null && j.Location.ToLower().Contains(request.Location.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.JobType))
        {
            query = query.Where(j => j.JobType == request.JobType);
        }

        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            query = query.Where(j => j.Company.ToLower().Contains(request.Company.ToLower()));
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var items = new List<JobDto>();
        foreach (var job in jobs)
        {
            items.Add(await MapToDto(job));
        }

        return new PagedResult<JobDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<JobDto> UpdateJobAsync(Guid userId, Guid jobId, CreateJobRequest request)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        if (job == null)
        {
            throw new KeyNotFoundException("Job not found");
        }

        job.Title = request.Title;
        job.Company = request.Company;
        job.Location = request.Location;
        job.Description = request.Description;
        job.Requirements = request.Requirements;
        job.Salary = request.Salary;
        job.JobType = request.JobType;
        job.SourceUrl = request.SourceUrl;
        job.SourcePlatform = request.SourcePlatform;
        job.PostedDate = request.PostedDate;
        job.ExpiryDate = request.ExpiryDate;

        await _context.SaveChangesAsync();

        // Trigger recommendation recomputation
        _ = Task.Run(async () => await _recommendationService.ComputeRecommendationsForJobAsync(job.Id));

        return await MapToDto(job);
    }

    public async Task DeleteJobAsync(Guid userId, Guid jobId)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        if (job == null)
        {
            throw new KeyNotFoundException("Job not found");
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
    }

    private static string? DetectPlatform(string url)
    {
        if (url.Contains("linkedin.com")) return "LinkedIn";
        if (url.Contains("indeed.com")) return "Indeed";
        if (url.Contains("glassdoor.com")) return "Glassdoor";
        if (url.Contains("monster.com")) return "Monster";
        if (url.Contains("ziprecruiter.com")) return "ZipRecruiter";
        if (url.Contains("dice.com")) return "Dice";
        if (url.Contains("angel.co") || url.Contains("wellfound.com")) return "AngelList";
        if (url.Contains("lever.co")) return "Lever";
        if (url.Contains("greenhouse.io")) return "Greenhouse";
        return null;
    }

    private async Task<JobDto> MapToDto(Job job, bool includeSourcePageHtml = false, List<ReminderDto>? reminders = null)
    {
        var application = job.Application ?? await _context.Applications
            .FirstOrDefaultAsync(a => a.JobId == job.Id);

        return new JobDto
        {
            Id = job.Id,
            Title = job.Title,
            Company = job.Company,
            Location = job.Location,
            Description = job.Description,
            Requirements = job.Requirements,
            Salary = job.Salary,
            JobType = job.JobType,
            SourceUrl = job.SourceUrl,
            SourcePageHtml = includeSourcePageHtml ? job.SourcePageHtml : null,
            SourcePlatform = job.SourcePlatform,
            PostedDate = job.PostedDate,
            ExpiryDate = job.ExpiryDate,
            IsExtracted = job.IsExtracted,
            HasApplication = application != null,
            ApplicationId = application?.Id,
            Reminders = reminders ?? new List<ReminderDto>(),
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }
}





