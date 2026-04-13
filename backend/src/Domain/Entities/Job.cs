namespace Joby.Domain.Entities;

public class Job : BaseEntity
{
    public Guid UserId { get; set; }
    
    // Job details
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Salary { get; set; }
    public string? JobType { get; set; } // Full-time, Part-time, Contract, Remote
    
    // Source information
    public string? SourceUrl { get; set; }
    /// <summary>Full HTML document from the listing URL (when added by URL).</summary>
    public string? SourcePageHtml { get; set; }
    public string? SourcePlatform { get; set; } // LinkedIn, Indeed, etc.
    public DateTime? PostedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Extracted data stored as JSON
    public string? ExtractedDataJson { get; set; }
    
    // Whether extraction was successful
    public bool IsExtracted { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public Application? Application { get; set; }
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}





