namespace Joby.Application.DTOs.Jobs;

/// <summary>
/// Result of fetching a job listing URL: parsed metadata plus the raw HTML document as returned by the server.
/// </summary>
public class JobScrapeResult
{
    public CreateJobRequest Metadata { get; set; } = new();
    public string RawPageHtml { get; set; } = string.Empty;
}
