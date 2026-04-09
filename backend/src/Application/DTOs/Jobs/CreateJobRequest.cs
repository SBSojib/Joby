namespace Joby.Application.DTOs.Jobs;

public class CreateJobRequest
{
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
}

public class CreateJobByUrlRequest
{
    public string Url { get; set; } = string.Empty;
}

public class JobSearchRequest
{
    public string? Query { get; set; }
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public string? Company { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}





