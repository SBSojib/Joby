using Joby.Domain.Enums;

namespace Joby.Domain.Entities;

public class ApplicationEvent : BaseEntity
{
    public Guid ApplicationId { get; set; }
    
    public string EventType { get; set; } = string.Empty; // StatusChange, Note, Interview, FollowUp, etc.
    public string? Description { get; set; }
    
    // For status changes
    public ApplicationStatus? OldStatus { get; set; }
    public ApplicationStatus? NewStatus { get; set; }
    
    // For scheduled events
    public DateTime? ScheduledAt { get; set; }
    
    // Navigation
    public Application Application { get; set; } = null!;
}





