using Joby.Domain.Enums;

namespace Joby.Domain.Entities;

public class Application : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;
    public DateTime? AppliedAt { get; set; }
    public string? Notes { get; set; }
    
    // Contact information
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    
    // Resume used for this application
    public Guid? ResumeId { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public ResumeDocument? Resume { get; set; }
    public ICollection<ApplicationEvent> Events { get; set; } = new List<ApplicationEvent>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}





