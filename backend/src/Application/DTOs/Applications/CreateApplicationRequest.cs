using Joby.Domain.Enums;

namespace Joby.Application.DTOs.Applications;

public class CreateApplicationRequest
{
    public Guid JobId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;
    public string? Notes { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public Guid? ResumeId { get; set; }
}

public class UpdateApplicationStatusRequest
{
    public ApplicationStatus Status { get; set; }
    public string? Note { get; set; }
}

public class AddApplicationEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class UpdateApplicationRequest
{
    public string? Notes { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}





