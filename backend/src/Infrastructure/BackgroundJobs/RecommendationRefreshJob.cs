using Joby.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Joby.Infrastructure.BackgroundJobs;

public class RecommendationRefreshJob
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationRefreshJob> _logger;

    public RecommendationRefreshJob(
        IRecommendationService recommendationService,
        ILogger<RecommendationRefreshJob> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting recommendation refresh job");

        try
        {
            await _recommendationService.ComputeAllRecommendationsAsync();
            _logger.LogInformation("Recommendation refresh job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in recommendation refresh job");
            throw;
        }
    }
}




