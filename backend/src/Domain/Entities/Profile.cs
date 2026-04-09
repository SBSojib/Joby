namespace Joby.Domain.Entities;

public class Profile : BaseEntity
{
    public Guid UserId { get; set; }
    
    // Extracted/Editable profile fields
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public string? CurrentTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    
    // Skills stored as JSON array
    public string SkillsJson { get; set; } = "[]";
    
    // Keywords for job matching stored as JSON array
    public string KeywordsJson { get; set; } = "[]";
    
    // Job preferences
    public string? PreferredLocations { get; set; }
    public string? PreferredJobTypes { get; set; } // Full-time, Part-time, Contract, Remote
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    
    // Work experience stored as JSON array
    public string WorkExperienceJson { get; set; } = "[]";
    
    // Education stored as JSON array
    public string EducationJson { get; set; } = "[]";
    
    // Active resume
    public Guid? ActiveResumeId { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public ResumeDocument? ActiveResume { get; set; }
}





