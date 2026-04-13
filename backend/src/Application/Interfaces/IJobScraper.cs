using Joby.Application.DTOs.Jobs;

namespace Joby.Application.Interfaces;
public interface IJobScraper
{
    /// <summary>Returns null when the URL could not be fetched. Otherwise returns metadata and the full response HTML.</summary>
    Task<JobScrapeResult?> ScrapeJobAsync(string url);
}





