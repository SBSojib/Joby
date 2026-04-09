namespace Joby.Domain.Entities;

public class Recommendation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    
    // Score (0-100)
    public double Score { get; set; }
    
    // Explanation fields stored as JSON arrays
    public string MatchedSkillsJson { get; set; } = "[]";
    public string MissingSkillsJson { get; set; } = "[]";
    public string MatchedKeywordsJson { get; set; } = "[]";
    public string MissingKeywordsJson { get; set; } = "[]";
    
    // Whether the user has seen this recommendation
    public bool IsSeen { get; set; }
    
    // Whether the recommendation is still valid
    public bool IsActive { get; set; } = true;
    
    // Last computed timestamp
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
    public Job Job { get; set; } = null!;
}





