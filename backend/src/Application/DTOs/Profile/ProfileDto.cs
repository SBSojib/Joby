namespace Joby.Application.DTOs.Profile;

public class ProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public string? CurrentTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public string? PreferredLocations { get; set; }
    public string? PreferredJobTypes { get; set; }
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public List<WorkExperienceDto> WorkExperience { get; set; } = new();
    public List<EducationDto> Education { get; set; } = new();
    public Guid? ActiveResumeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkExperienceDto
{
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsCurrent { get; set; }
}

public class EducationDto
{
    public string? Degree { get; set; }
    public string? School { get; set; }
    public string? Field { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
}





