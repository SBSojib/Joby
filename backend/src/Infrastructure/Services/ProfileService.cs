using System.Text.Json;
using Joby.Application.DTOs.Profile;
using Joby.Application.Interfaces;
using Joby.Domain.Entities;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Joby.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IResumeParser _resumeParser;
    private readonly IRecommendationService _recommendationService;

    public ProfileService(
        ApplicationDbContext context,
        IResumeParser resumeParser,
        IRecommendationService recommendationService)
    {
        _context = context;
        _resumeParser = resumeParser;
        _recommendationService = recommendationService;
    }

    public async Task<ProfileDto> GetProfileAsync(Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            // Create a default profile if not exists
            profile = new Profile { UserId = userId };
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        return MapToDto(profile);
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new Profile { UserId = userId };
            _context.Profiles.Add(profile);
        }

        // Update fields
        if (request.FullName != null) profile.FullName = request.FullName;
        if (request.Email != null) profile.Email = request.Email;
        if (request.Phone != null) profile.Phone = request.Phone;
        if (request.Location != null) profile.Location = request.Location;
        if (request.Summary != null) profile.Summary = request.Summary;
        if (request.CurrentTitle != null) profile.CurrentTitle = request.CurrentTitle;
        if (request.YearsOfExperience.HasValue) profile.YearsOfExperience = request.YearsOfExperience;
        if (request.Skills != null) profile.SkillsJson = JsonSerializer.Serialize(request.Skills);
        if (request.Keywords != null) profile.KeywordsJson = JsonSerializer.Serialize(request.Keywords);
        if (request.PreferredLocations != null) profile.PreferredLocations = request.PreferredLocations;
        if (request.PreferredJobTypes != null) profile.PreferredJobTypes = request.PreferredJobTypes;
        if (request.MinSalary.HasValue) profile.MinSalary = request.MinSalary;
        if (request.MaxSalary.HasValue) profile.MaxSalary = request.MaxSalary;
        if (request.WorkExperience != null) profile.WorkExperienceJson = JsonSerializer.Serialize(request.WorkExperience);
        if (request.Education != null) profile.EducationJson = JsonSerializer.Serialize(request.Education);

        await _context.SaveChangesAsync();

        // Trigger recommendation refresh
        _ = Task.Run(async () => await _recommendationService.ComputeRecommendationsForUserAsync(userId));

        return MapToDto(profile);
    }

    public async Task<ResumeDto> UploadResumeAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        // Read the file into memory
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();

        // Parse the resume
        memoryStream.Position = 0;
        string? extractedText = null;
        ParsedResumeData? parsedData = null;
        string? parseError = null;
        bool isParsed = false;

        try
        {
            var (text, data) = await _resumeParser.ParseAsync(memoryStream, fileName, contentType);
            extractedText = text;
            parsedData = data;
            isParsed = true;
        }
        catch (Exception ex)
        {
            parseError = ex.Message;
        }

        var resume = new ResumeDocument
        {
            UserId = userId,
            FileName = fileName,
            ContentType = contentType,
            FileData = fileData,
            FileSize = fileData.Length,
            ExtractedText = extractedText,
            ParsedDataJson = parsedData != null ? JsonSerializer.Serialize(parsedData) : null,
            IsParsed = isParsed,
            ParseError = parseError
        };

        _context.ResumeDocuments.Add(resume);
        await _context.SaveChangesAsync();

        // Set as active resume if this is the first one
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile != null && profile.ActiveResumeId == null)
        {
            profile.ActiveResumeId = resume.Id;
            await _context.SaveChangesAsync();
        }

        return MapToResumeDto(resume);
    }

    public async Task<List<ResumeDto>> GetResumesAsync(Guid userId)
    {
        var resumes = await _context.ResumeDocuments
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return resumes.Select(MapToResumeDto).ToList();
    }

    public async Task<ResumeDto?> GetResumeAsync(Guid userId, Guid resumeId)
    {
        var resume = await _context.ResumeDocuments
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

        return resume != null ? MapToResumeDto(resume) : null;
    }

    public async Task<byte[]?> GetResumeFileAsync(Guid userId, Guid resumeId)
    {
        var resume = await _context.ResumeDocuments
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

        return resume?.FileData;
    }

    public async Task SetActiveResumeAsync(Guid userId, Guid resumeId)
    {
        var resume = await _context.ResumeDocuments
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

        if (resume == null)
        {
            throw new KeyNotFoundException("Resume not found");
        }

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new Profile { UserId = userId };
            _context.Profiles.Add(profile);
        }

        profile.ActiveResumeId = resumeId;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteResumeAsync(Guid userId, Guid resumeId)
    {
        var resume = await _context.ResumeDocuments
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

        if (resume == null)
        {
            throw new KeyNotFoundException("Resume not found");
        }

        // Update profile if this was the active resume
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile?.ActiveResumeId == resumeId)
        {
            profile.ActiveResumeId = null;
        }

        _context.ResumeDocuments.Remove(resume);
        await _context.SaveChangesAsync();
    }

    private static ProfileDto MapToDto(Profile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FullName = profile.FullName,
        Email = profile.Email,
        Phone = profile.Phone,
        Location = profile.Location,
        Summary = profile.Summary,
        CurrentTitle = profile.CurrentTitle,
        YearsOfExperience = profile.YearsOfExperience,
        Skills = JsonSerializer.Deserialize<List<string>>(profile.SkillsJson) ?? new(),
        Keywords = JsonSerializer.Deserialize<List<string>>(profile.KeywordsJson) ?? new(),
        PreferredLocations = profile.PreferredLocations,
        PreferredJobTypes = profile.PreferredJobTypes,
        MinSalary = profile.MinSalary,
        MaxSalary = profile.MaxSalary,
        WorkExperience = JsonSerializer.Deserialize<List<WorkExperienceDto>>(profile.WorkExperienceJson) ?? new(),
        Education = JsonSerializer.Deserialize<List<EducationDto>>(profile.EducationJson) ?? new(),
        ActiveResumeId = profile.ActiveResumeId,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };

    private static ResumeDto MapToResumeDto(ResumeDocument resume) => new()
    {
        Id = resume.Id,
        FileName = resume.FileName,
        ContentType = resume.ContentType,
        FileSize = resume.FileSize,
        IsParsed = resume.IsParsed,
        ParseError = resume.ParseError,
        ParsedData = !string.IsNullOrEmpty(resume.ParsedDataJson)
            ? JsonSerializer.Deserialize<ParsedResumeData>(resume.ParsedDataJson)
            : null,
        CreatedAt = resume.CreatedAt
    };
}





