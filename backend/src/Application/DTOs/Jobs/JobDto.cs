namespace Joby.Application.DTOs.Jobs;

public class JobDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Salary { get; set; }
    public string? JobType { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourcePlatform { get; set; }
    public DateTime? PostedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExtracted { get; set; }
    public bool HasApplication { get; set; }
    public Guid? ApplicationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class JobWithRecommendationDto : JobDto
{
    public double? RecommendationScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> MatchedKeywords { get; set; } = new();
    public List<string> MissingKeywords { get; set; } = new();
}





