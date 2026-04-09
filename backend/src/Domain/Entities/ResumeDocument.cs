namespace Joby.Domain.Entities;

public class ResumeDocument : BaseEntity
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
    
    // Extracted raw text
    public string? ExtractedText { get; set; }
    
    // Parsed fields (JSON)
    public string? ParsedDataJson { get; set; }
    
    // Parsing status
    public bool IsParsed { get; set; }
    public string? ParseError { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}





