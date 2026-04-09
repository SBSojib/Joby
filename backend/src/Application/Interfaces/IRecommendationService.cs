using Joby.Application.DTOs.Jobs;

namespace Joby.Application.Interfaces;

public interface IRecommendationService
{
    Task ComputeRecommendationsForUserAsync(Guid userId);
    Task ComputeRecommendationsForJobAsync(Guid jobId);
    Task ComputeAllRecommendationsAsync();
    Task<List<JobWithRecommendationDto>> GetTopRecommendationsAsync(Guid userId, int count = 10);
}





