using Joby.Application.DTOs.Common;
using Joby.Application.DTOs.Jobs;

namespace Joby.Application.Interfaces;

public interface IJobService
{
    Task<JobDto> CreateJobAsync(Guid userId, CreateJobRequest request);
    Task<JobDto> CreateJobByUrlAsync(Guid userId, string url);
    Task<JobDto?> GetJobAsync(Guid userId, Guid jobId);
    Task<PagedResult<JobWithRecommendationDto>> GetRecommendedJobsAsync(Guid userId, int page, int pageSize);
    Task<PagedResult<JobDto>> SearchJobsAsync(Guid userId, JobSearchRequest request);
    Task<JobDto> UpdateJobAsync(Guid userId, Guid jobId, CreateJobRequest request);
    Task DeleteJobAsync(Guid userId, Guid jobId);
}





