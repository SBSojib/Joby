namespace Joby.Application.DTOs.Profile;

public class ResumeDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsParsed { get; set; }
    public string? ParseError { get; set; }
    public ParsedResumeData? ParsedData { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ParsedResumeData
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Summary { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> JobTitles { get; set; } = new();
    public List<WorkExperienceDto> WorkExperience { get; set; } = new();
    public List<EducationDto> Education { get; set; } = new();
}





