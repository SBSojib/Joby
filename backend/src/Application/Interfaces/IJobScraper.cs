using Joby.Application.DTOs.Jobs;

namespace Joby.Application.Interfaces;

public interface IJobScraper
{
    Task<CreateJobRequest?> ScrapeJobAsync(string url);
}





