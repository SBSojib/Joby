namespace Joby.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    /// <summary>When set, the session owner (<see cref="UserId"/>) is acting as this user.</summary>
    public Guid? ImpersonatedUserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string? RevokedByIp { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation
    public User User { get; set; } = null!;
    public User? ImpersonatedUser { get; set; }
}





