using System.Text.Json;
using Joby.Domain.Entities;
using Joby.Infrastructure.Persistence;
using Joby.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Joby.Tests;

public class RecommendationServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<RecommendationService>>();
        _service = new RecommendationService(_context, logger.Object);
    }

    [Fact]
    public async Task ComputeRecommendationsForJob_WithMatchingSkills_ReturnsHighScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };

        var profile = new Profile
        {
            UserId = userId,
            SkillsJson = JsonSerializer.Serialize(new List<string> { "C#", ".NET", "React", "TypeScript" }),
            KeywordsJson = JsonSerializer.Serialize(new List<string> { "software engineer", "full stack", "backend" })
        };

        var job = new Job
        {
            UserId = userId,
            Title = "Senior Software Engineer",
            Company = "TechCorp",
            Description = "Looking for a full stack developer with C#, .NET, React, and TypeScript experience. Backend development skills required."
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        await _service.ComputeRecommendationsForJobAsync(job.Id);

        // Assert
        var recommendation = await _context.Recommendations
            .FirstOrDefaultAsync(r => r.JobId == job.Id && r.UserId == userId);

        Assert.NotNull(recommendation);
        Assert.True(recommendation.Score > 50); // High score due to matching skills
        
        var matchedSkills = JsonSerializer.Deserialize<List<string>>(recommendation.MatchedSkillsJson);
        Assert.NotNull(matchedSkills);
        Assert.Contains("C#", matchedSkills);
        Assert.Contains("React", matchedSkills);
    }

    [Fact]
    public async Task ComputeRecommendationsForJob_WithNoMatchingSkills_ReturnsLowScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test2@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };

        var profile = new Profile
        {
            UserId = userId,
            SkillsJson = JsonSerializer.Serialize(new List<string> { "Python", "Django", "PostgreSQL" }),
            KeywordsJson = JsonSerializer.Serialize(new List<string> { "data science", "machine learning" })
        };

        var job = new Job
        {
            UserId = userId,
            Title = "iOS Developer",
            Company = "MobileCorp",
            Description = "Looking for an iOS developer with Swift and SwiftUI experience. Mobile development required."
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        await _service.ComputeRecommendationsForJobAsync(job.Id);

        // Assert
        var recommendation = await _context.Recommendations
            .FirstOrDefaultAsync(r => r.JobId == job.Id && r.UserId == userId);

        Assert.NotNull(recommendation);
        Assert.True(recommendation.Score < 50); // Low score due to no matching skills
        
        var matchedSkills = JsonSerializer.Deserialize<List<string>>(recommendation.MatchedSkillsJson);
        Assert.NotNull(matchedSkills);
        Assert.Empty(matchedSkills);
    }

    [Fact]
    public async Task ComputeRecommendationsForUser_CreatesRecommendationsForAllJobs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test3@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };

        var profile = new Profile
        {
            UserId = userId,
            SkillsJson = JsonSerializer.Serialize(new List<string> { "JavaScript", "React" }),
            KeywordsJson = JsonSerializer.Serialize(new List<string> { "frontend" })
        };

        var job1 = new Job
        {
            UserId = userId,
            Title = "Frontend Developer",
            Company = "Company1",
            Description = "React and JavaScript required"
        };

        var job2 = new Job
        {
            UserId = userId,
            Title = "Backend Developer",
            Company = "Company2",
            Description = "Node.js and Express required"
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        _context.Jobs.AddRange(job1, job2);
        await _context.SaveChangesAsync();

        // Act
        await _service.ComputeRecommendationsForUserAsync(userId);

        // Assert
        var recommendations = await _context.Recommendations
            .Where(r => r.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, recommendations.Count);
    }

    [Fact]
    public async Task GetTopRecommendations_ReturnsJobsSortedByScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test4@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };

        var profile = new Profile
        {
            UserId = userId,
            SkillsJson = JsonSerializer.Serialize(new List<string> { "C#", ".NET" }),
            KeywordsJson = JsonSerializer.Serialize(new List<string> { "backend" })
        };

        var job1 = new Job
        {
            UserId = userId,
            Title = "C# Developer",
            Company = "Company1",
            Description = "C# and .NET backend developer needed"
        };

        var job2 = new Job
        {
            UserId = userId,
            Title = "Python Developer",
            Company = "Company2",
            Description = "Python developer needed"
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        _context.Jobs.AddRange(job1, job2);
        await _context.SaveChangesAsync();

        await _service.ComputeRecommendationsForUserAsync(userId);

        // Act
        var recommendations = await _service.GetTopRecommendationsAsync(userId, 10);

        // Assert
        Assert.Equal(2, recommendations.Count);
        Assert.True(recommendations[0].RecommendationScore >= recommendations[1].RecommendationScore);
        Assert.Equal("C# Developer", recommendations[0].Title);
    }
}




