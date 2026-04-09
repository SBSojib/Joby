namespace Joby.Application.DTOs.Profile;

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public string? CurrentTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<string>? Skills { get; set; }
    public List<string>? Keywords { get; set; }
    public string? PreferredLocations { get; set; }
    public string? PreferredJobTypes { get; set; }
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public List<WorkExperienceDto>? WorkExperience { get; set; }
    public List<EducationDto>? Education { get; set; }
}





