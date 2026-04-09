using System.Text.Json;
using System.Text.RegularExpressions;
using Joby.Application.DTOs.Jobs;
using Joby.Application.Interfaces;
using Joby.Domain.Entities;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Joby.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(ApplicationDbContext context, ILogger<RecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ComputeRecommendationsForUserAsync(Guid userId)
    {
        try
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return;

            var userSkills = JsonSerializer.Deserialize<List<string>>(profile.SkillsJson) ?? new();
            var userKeywords = JsonSerializer.Deserialize<List<string>>(profile.KeywordsJson) ?? new();

            // Get all jobs (we could filter to only jobs without applications)
            var jobs = await _context.Jobs
                .Where(j => j.Application == null) // Only jobs user hasn't applied to
                .ToListAsync();

            foreach (var job in jobs)
            {
                await ComputeAndSaveRecommendation(userId, job, userSkills, userKeywords);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing recommendations for user {UserId}", userId);
        }
    }

    public async Task ComputeRecommendationsForJobAsync(Guid jobId)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return;

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == job.UserId);
            var userSkills = profile != null
                ? JsonSerializer.Deserialize<List<string>>(profile.SkillsJson) ?? new()
                : new List<string>();
            var userKeywords = profile != null
                ? JsonSerializer.Deserialize<List<string>>(profile.KeywordsJson) ?? new()
                : new List<string>();

            await ComputeAndSaveRecommendation(job.UserId, job, userSkills, userKeywords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing recommendations for job {JobId}", jobId);
        }
    }

    public async Task ComputeAllRecommendationsAsync()
    {
        try
        {
            var users = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var userId in users)
            {
                await ComputeRecommendationsForUserAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing all recommendations");
        }
    }

    public async Task<List<JobWithRecommendationDto>> GetTopRecommendationsAsync(Guid userId, int count = 10)
    {
        var recommendations = await _context.Recommendations
            .Include(r => r.Job)
                .ThenInclude(j => j.Application)
            .Where(r => r.UserId == userId && r.IsActive && r.Job.Application == null)
            .OrderByDescending(r => r.Score)
            .Take(count)
            .ToListAsync();

        return recommendations.Select(r => new JobWithRecommendationDto
        {
            Id = r.Job.Id,
            Title = r.Job.Title,
            Company = r.Job.Company,
            Location = r.Job.Location,
            Description = r.Job.Description,
            Requirements = r.Job.Requirements,
            Salary = r.Job.Salary,
            JobType = r.Job.JobType,
            SourceUrl = r.Job.SourceUrl,
            SourcePlatform = r.Job.SourcePlatform,
            PostedDate = r.Job.PostedDate,
            ExpiryDate = r.Job.ExpiryDate,
            IsExtracted = r.Job.IsExtracted,
            HasApplication = r.Job.Application != null,
            ApplicationId = r.Job.Application?.Id,
            CreatedAt = r.Job.CreatedAt,
            UpdatedAt = r.Job.UpdatedAt,
            RecommendationScore = r.Score,
            MatchedSkills = JsonSerializer.Deserialize<List<string>>(r.MatchedSkillsJson) ?? new(),
            MissingSkills = JsonSerializer.Deserialize<List<string>>(r.MissingSkillsJson) ?? new(),
            MatchedKeywords = JsonSerializer.Deserialize<List<string>>(r.MatchedKeywordsJson) ?? new(),
            MissingKeywords = JsonSerializer.Deserialize<List<string>>(r.MissingKeywordsJson) ?? new()
        }).ToList();
    }

    private async Task ComputeAndSaveRecommendation(
        Guid userId,
        Job job,
        List<string> userSkills,
        List<string> userKeywords)
    {
        // Extract keywords from job
        var jobText = $"{job.Title} {job.Description} {job.Requirements}".ToLower();
        var jobKeywords = ExtractKeywords(jobText);

        // Compute matches
        var matchedSkills = userSkills
            .Where(s => jobText.Contains(s.ToLower()))
            .ToList();

        var missingSkills = ExtractRequiredSkills(job.Requirements ?? job.Description ?? "")
            .Where(s => !userSkills.Any(us => us.Equals(s, StringComparison.OrdinalIgnoreCase)))
            .Take(10)
            .ToList();

        var matchedKeywords = userKeywords
            .Where(k => jobText.Contains(k.ToLower()))
            .ToList();

        var missingKeywords = jobKeywords
            .Where(k => !userKeywords.Any(uk => uk.Equals(k, StringComparison.OrdinalIgnoreCase)))
            .Take(10)
            .ToList();

        // Compute score (0-100)
        var skillScore = userSkills.Count > 0
            ? (double)matchedSkills.Count / userSkills.Count * 50
            : 25;

        var keywordScore = userKeywords.Count > 0
            ? (double)matchedKeywords.Count / userKeywords.Count * 30
            : 15;

        // Bonus for title match
        var titleBonus = userKeywords.Any(k => job.Title.ToLower().Contains(k.ToLower())) ? 20 : 0;

        var totalScore = Math.Min(100, skillScore + keywordScore + titleBonus);

        // Find or create recommendation
        var recommendation = await _context.Recommendations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == job.Id);

        if (recommendation == null)
        {
            recommendation = new Recommendation
            {
                UserId = userId,
                JobId = job.Id
            };
            _context.Recommendations.Add(recommendation);
        }

        recommendation.Score = totalScore;
        recommendation.MatchedSkillsJson = JsonSerializer.Serialize(matchedSkills);
        recommendation.MissingSkillsJson = JsonSerializer.Serialize(missingSkills);
        recommendation.MatchedKeywordsJson = JsonSerializer.Serialize(matchedKeywords);
        recommendation.MissingKeywordsJson = JsonSerializer.Serialize(missingKeywords);
        recommendation.ComputedAt = DateTime.UtcNow;
        recommendation.IsActive = true;

        await _context.SaveChangesAsync();
    }

    private static List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction - find common tech/job keywords
        var commonKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "python", "javascript", "typescript", "react", "angular", "vue", "node", "nodejs",
            "java", "c#", "csharp", ".net", "dotnet", "go", "golang", "rust", "ruby", "php",
            "sql", "nosql", "mongodb", "postgresql", "mysql", "redis", "elasticsearch",
            "aws", "azure", "gcp", "docker", "kubernetes", "k8s", "terraform", "ansible",
            "ci/cd", "devops", "agile", "scrum", "rest", "api", "graphql", "microservices",
            "machine learning", "ml", "ai", "data science", "analytics", "big data",
            "frontend", "backend", "fullstack", "full-stack", "mobile", "ios", "android",
            "linux", "git", "jenkins", "github", "gitlab", "jira", "confluence",
            "senior", "junior", "lead", "principal", "staff", "architect", "manager"
        };

        var words = Regex.Split(text.ToLower(), @"\W+")
            .Where(w => w.Length > 2)
            .ToList();

        return words
            .Where(w => commonKeywords.Contains(w))
            .Distinct()
            .ToList();
    }

    private static List<string> ExtractRequiredSkills(string text)
    {
        // Common technical skills to look for
        var skillPatterns = new[]
        {
            @"\b(python|javascript|typescript|java|c#|c\+\+|go|rust|ruby|php|swift|kotlin)\b",
            @"\b(react|angular|vue|svelte|next\.?js|nuxt)\b",
            @"\b(node\.?js|express|fastapi|django|flask|spring|\.net)\b",
            @"\b(sql|postgresql|mysql|mongodb|redis|elasticsearch)\b",
            @"\b(aws|azure|gcp|docker|kubernetes)\b",
            @"\b(git|ci\/cd|devops|agile)\b"
        };

        var skills = new List<string>();

        foreach (var pattern in skillPatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            skills.AddRange(matches.Select(m => m.Value));
        }

        return skills.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
