using Joby.Application.DTOs.Applications;
using Joby.Application.DTOs.Jobs;
using Joby.Application.Interfaces;
using Joby.Domain.Entities;
using Joby.Domain.Enums;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Joby.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly IReminderService _reminderService;

    public ApplicationService(ApplicationDbContext context, IReminderService reminderService)
    {
        _context = context;
        _reminderService = reminderService;
    }

    public async Task<ApplicationDto> CreateApplicationAsync(Guid userId, CreateApplicationRequest request)
    {
        // Verify job belongs to user
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId && j.UserId == userId);

        if (job == null)
        {
            throw new KeyNotFoundException("Job not found");
        }

        // Check if application already exists
        var existingApp = await _context.Applications
            .FirstOrDefaultAsync(a => a.JobId == request.JobId && a.UserId == userId);

        if (existingApp != null)
        {
            throw new InvalidOperationException("Application already exists for this job");
        }

        var application = new Domain.Entities.Application
        {
            UserId = userId,
            JobId = request.JobId,
            Status = request.Status,
            Notes = request.Notes,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            ResumeId = request.ResumeId,
            AppliedAt = request.Status >= ApplicationStatus.Applied ? DateTime.UtcNow : null
        };

        _context.Applications.Add(application);

        // Create initial status event
        var statusEvent = new ApplicationEvent
        {
            ApplicationId = application.Id,
            EventType = "StatusChange",
            Description = $"Application created with status: {request.Status}",
            NewStatus = request.Status
        };

        _context.ApplicationEvents.Add(statusEvent);
        await _context.SaveChangesAsync();

        // Create follow-up reminder if status is Applied
        if (request.Status >= ApplicationStatus.Applied)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await _reminderService.CreateFollowUpReminderAsync(userId, application.Id, user.DefaultFollowUpDays);
            }
        }

        return await GetApplicationDtoAsync(application.Id, userId);
    }

    public async Task<ApplicationDto?> GetApplicationAsync(Guid userId, Guid applicationId)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        return application != null ? await GetApplicationDtoAsync(applicationId, userId) : null;
    }

    public async Task<List<ApplicationDto>> GetApplicationsAsync(Guid userId)
    {
        var applications = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.Events)
            .Include(a => a.Reminders)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();

        return applications.Select(MapToDto).ToList();
    }

    public async Task<Dictionary<ApplicationStatus, List<ApplicationDto>>> GetApplicationsPipelineAsync(Guid userId)
    {
        var applications = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.Events)
            .Include(a => a.Reminders)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();

        var pipeline = new Dictionary<ApplicationStatus, List<ApplicationDto>>();

        foreach (ApplicationStatus status in Enum.GetValues<ApplicationStatus>())
        {
            pipeline[status] = applications
                .Where(a => a.Status == status)
                .Select(MapToDto)
                .ToList();
        }

        return pipeline;
    }

    public async Task<ApplicationDto> UpdateStatusAsync(Guid userId, Guid applicationId, UpdateApplicationStatusRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found");
        }

        var oldStatus = application.Status;
        application.Status = request.Status;

        // Update AppliedAt if transitioning to Applied
        if (oldStatus < ApplicationStatus.Applied && request.Status >= ApplicationStatus.Applied)
        {
            application.AppliedAt = DateTime.UtcNow;

            // Create follow-up reminder
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await _reminderService.CreateFollowUpReminderAsync(userId, applicationId, user.DefaultFollowUpDays);
            }
        }

        // Create status change event
        var statusEvent = new ApplicationEvent
        {
            ApplicationId = applicationId,
            EventType = "StatusChange",
            Description = request.Note ?? $"Status changed from {oldStatus} to {request.Status}",
            OldStatus = oldStatus,
            NewStatus = request.Status
        };

        _context.ApplicationEvents.Add(statusEvent);
        await _context.SaveChangesAsync();

        return await GetApplicationDtoAsync(applicationId, userId);
    }

    public async Task<ApplicationDto> UpdateApplicationAsync(Guid userId, Guid applicationId, UpdateApplicationRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found");
        }

        if (request.Notes != null) application.Notes = request.Notes;
        if (request.ContactName != null) application.ContactName = request.ContactName;
        if (request.ContactEmail != null) application.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) application.ContactPhone = request.ContactPhone;

        await _context.SaveChangesAsync();

        return await GetApplicationDtoAsync(applicationId, userId);
    }

    public async Task<ApplicationEventDto> AddEventAsync(Guid userId, Guid applicationId, AddApplicationEventRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found");
        }

        var appEvent = new ApplicationEvent
        {
            ApplicationId = applicationId,
            EventType = request.EventType,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt
        };

        _context.ApplicationEvents.Add(appEvent);
        await _context.SaveChangesAsync();

        return new ApplicationEventDto
        {
            Id = appEvent.Id,
            EventType = appEvent.EventType,
            Description = appEvent.Description,
            ScheduledAt = appEvent.ScheduledAt,
            CreatedAt = appEvent.CreatedAt
        };
    }

    public async Task DeleteApplicationAsync(Guid userId, Guid applicationId)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found");
        }

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
    }

    private async Task<ApplicationDto> GetApplicationDtoAsync(Guid applicationId, Guid userId)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.Events)
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        return MapToDto(application!);
    }

    private static ApplicationDto MapToDto(Domain.Entities.Application application)
    {
        return new ApplicationDto
        {
            Id = application.Id,
            JobId = application.JobId,
            Job = new JobDto
            {
                Id = application.Job.Id,
                Title = application.Job.Title,
                Company = application.Job.Company,
                Location = application.Job.Location,
                Description = application.Job.Description,
                Requirements = application.Job.Requirements,
                Salary = application.Job.Salary,
                JobType = application.Job.JobType,
                SourceUrl = application.Job.SourceUrl,
                SourcePlatform = application.Job.SourcePlatform,
                PostedDate = application.Job.PostedDate,
                ExpiryDate = application.Job.ExpiryDate,
                IsExtracted = application.Job.IsExtracted,
                HasApplication = true,
                ApplicationId = application.Id,
                CreatedAt = application.Job.CreatedAt,
                UpdatedAt = application.Job.UpdatedAt
            },
            Status = application.Status,
            AppliedAt = application.AppliedAt,
            Notes = application.Notes,
            ContactName = application.ContactName,
            ContactEmail = application.ContactEmail,
            ContactPhone = application.ContactPhone,
            ResumeId = application.ResumeId,
            Events = application.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ApplicationEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Description = e.Description,
                    OldStatus = e.OldStatus,
                    NewStatus = e.NewStatus,
                    ScheduledAt = e.ScheduledAt,
                    CreatedAt = e.CreatedAt
                }).ToList(),
            Reminders = application.Reminders
                .OrderBy(r => r.DueAt)
                .Select(r => new ReminderDto
                {
                    Id = r.Id,
                    ApplicationId = r.ApplicationId,
                    Title = r.Title,
                    Description = r.Description,
                    DueAt = r.DueAt,
                    Status = r.Status,
                    CompletedAt = r.CompletedAt,
                    SnoozedUntil = r.SnoozedUntil,
                    IsAutoGenerated = r.IsAutoGenerated,
                    ReminderType = r.ReminderType,
                    CreatedAt = r.CreatedAt
                }).ToList(),
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}





