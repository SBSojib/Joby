namespace Joby.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationCodeExpiresAt { get; set; }
    public DateTime? EmailVerificationCodeSentAt { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }
    public DateTime? PasswordResetCodeSentAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // User preferences
    public int DefaultFollowUpDays { get; set; } = 7;
    
    // Navigation properties
    public Profile? Profile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ResumeDocument> Resumes { get; set; } = new List<ResumeDocument>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}





