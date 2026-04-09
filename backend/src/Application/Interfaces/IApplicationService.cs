using Joby.Application.DTOs.Applications;
using Joby.Application.DTOs.Common;
using Joby.Domain.Enums;

namespace Joby.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> CreateApplicationAsync(Guid userId, CreateApplicationRequest request);
    Task<ApplicationDto?> GetApplicationAsync(Guid userId, Guid applicationId);
    Task<List<ApplicationDto>> GetApplicationsAsync(Guid userId);
    Task<Dictionary<ApplicationStatus, List<ApplicationDto>>> GetApplicationsPipelineAsync(Guid userId);
    Task<ApplicationDto> UpdateStatusAsync(Guid userId, Guid applicationId, UpdateApplicationStatusRequest request);
    Task<ApplicationDto> UpdateApplicationAsync(Guid userId, Guid applicationId, UpdateApplicationRequest request);
    Task<ApplicationEventDto> AddEventAsync(Guid userId, Guid applicationId, AddApplicationEventRequest request);
    Task DeleteApplicationAsync(Guid userId, Guid applicationId);
}





