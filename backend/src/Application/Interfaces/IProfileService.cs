using Joby.Application.DTOs.Profile;

namespace Joby.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(Guid userId);
    Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<ResumeDto> UploadResumeAsync(Guid userId, Stream fileStream, string fileName, string contentType);
    Task<List<ResumeDto>> GetResumesAsync(Guid userId);
    Task<ResumeDto?> GetResumeAsync(Guid userId, Guid resumeId);
    Task<byte[]?> GetResumeFileAsync(Guid userId, Guid resumeId);
    Task SetActiveResumeAsync(Guid userId, Guid resumeId);
    Task DeleteResumeAsync(Guid userId, Guid resumeId);
}





