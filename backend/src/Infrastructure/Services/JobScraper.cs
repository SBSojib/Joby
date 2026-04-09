using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Joby.Application.DTOs.Jobs;
using Joby.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Joby.Infrastructure.Services;

public class JobScraper : IJobScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobScraper> _logger;

    public JobScraper(HttpClient httpClient, ILogger<JobScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateJobRequest?> ScrapeJobAsync(string url)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch URL {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var job = new CreateJobRequest
            {
                SourceUrl = url
            };

            // Try structured data first (JSON-LD)
            var jsonLdScript = doc.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']");
            if (jsonLdScript != null)
            {
                var parsedFromJsonLd = TryParseJsonLd(jsonLdScript.InnerText, job);
                if (parsedFromJsonLd)
                {
                    return job;
                }
            }

            // Fall back to meta tags and common patterns
            ExtractFromMetaTags(doc, job);
            ExtractFromCommonPatterns(doc, job);

            // Basic validation
            if (string.IsNullOrWhiteSpace(job.Title))
            {
                job.Title = ExtractTitle(doc) ?? "Unknown Position";
            }

            if (string.IsNullOrWhiteSpace(job.Company))
            {
                job.Company = ExtractCompany(doc, url) ?? "Unknown Company";
            }

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping job from URL {Url}", url);
            return null;
        }
    }

    private bool TryParseJsonLd(string json, CreateJobRequest job)
    {
        try
        {
            // Simple JSON parsing for job posting schema
            if (json.Contains("\"@type\"") && json.Contains("JobPosting"))
            {
                // Extract title
                var titleMatch = Regex.Match(json, @"""title""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (titleMatch.Success) job.Title = HtmlDecode(titleMatch.Groups[1].Value);

                // Extract company
                var companyMatch = Regex.Match(json, @"""hiringOrganization""[^}]*""name""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (companyMatch.Success) job.Company = HtmlDecode(companyMatch.Groups[1].Value);

                // Extract location
                var locationMatch = Regex.Match(json, @"""jobLocation""[^}]*""addressLocality""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (locationMatch.Success) job.Location = HtmlDecode(locationMatch.Groups[1].Value);

                // Extract description
                var descMatch = Regex.Match(json, @"""description""\s*:\s*""([^""]{10,})""", RegexOptions.IgnoreCase);
                if (descMatch.Success) job.Description = CleanDescription(HtmlDecode(descMatch.Groups[1].Value));

                // Extract salary
                var salaryMatch = Regex.Match(json, @"""baseSalary""[^}]*""value""\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (salaryMatch.Success) job.Salary = salaryMatch.Groups[1].Value;

                // Extract employment type
                var typeMatch = Regex.Match(json, @"""employmentType""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (typeMatch.Success) job.JobType = HtmlDecode(typeMatch.Groups[1].Value);

                return !string.IsNullOrWhiteSpace(job.Title);
            }
        }
        catch (Exception)
        {
            // JSON-LD parsing failed, will fall back to other methods
        }

        return false;
    }

    private void ExtractFromMetaTags(HtmlDocument doc, CreateJobRequest job)
    {
        // Open Graph and Twitter meta tags
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null);
        var ogDescription = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null);

        if (!string.IsNullOrWhiteSpace(ogTitle) && string.IsNullOrWhiteSpace(job.Title))
        {
            job.Title = ogTitle;
        }

        if (!string.IsNullOrWhiteSpace(ogDescription) && string.IsNullOrWhiteSpace(job.Description))
        {
            job.Description = ogDescription;
        }
    }

    private void ExtractFromCommonPatterns(HtmlDocument doc, CreateJobRequest job)
    {
        // Common CSS class patterns for job titles
        var titleSelectors = new[]
        {
            "//h1[contains(@class, 'job-title')]",
            "//h1[contains(@class, 'jobTitle')]",
            "//h1[contains(@class, 'position')]",
            "//h1[@data-testid='job-title']",
            "//div[contains(@class, 'job-title')]//h1",
            "//h1"
        };

        foreach (var selector in titleSelectors)
        {
            if (!string.IsNullOrWhiteSpace(job.Title)) break;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                job.Title = CleanText(node.InnerText);
            }
        }

        // Common CSS class patterns for company names
        var companySelectors = new[]
        {
            "//a[contains(@class, 'company')]",
            "//span[contains(@class, 'company')]",
            "//div[contains(@class, 'company-name')]",
            "//span[@data-testid='company-name']"
        };

        foreach (var selector in companySelectors)
        {
            if (!string.IsNullOrWhiteSpace(job.Company)) break;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                job.Company = CleanText(node.InnerText);
            }
        }

        // Location patterns
        var locationSelectors = new[]
        {
            "//span[contains(@class, 'location')]",
            "//div[contains(@class, 'location')]",
            "//span[@data-testid='location']"
        };

        foreach (var selector in locationSelectors)
        {
            if (!string.IsNullOrWhiteSpace(job.Location)) break;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                job.Location = CleanText(node.InnerText);
            }
        }

        // Description patterns
        var descriptionSelectors = new[]
        {
            "//div[contains(@class, 'job-description')]",
            "//div[contains(@class, 'description')]",
            "//section[contains(@class, 'description')]",
            "//div[@id='job-description']"
        };

        foreach (var selector in descriptionSelectors)
        {
            if (!string.IsNullOrWhiteSpace(job.Description)) break;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                job.Description = CleanDescription(node.InnerText);
            }
        }
    }

    private string? ExtractTitle(HtmlDocument doc)
    {
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            var title = CleanText(titleNode.InnerText);
            // Remove common suffixes
            title = Regex.Replace(title, @"\s*[-|]\s*.*$", "").Trim();
            return title;
        }

        return null;
    }

    private string? ExtractCompany(HtmlDocument doc, string url)
    {
        // Try to extract from URL
        var uri = new Uri(url);
        var host = uri.Host.Replace("www.", "").Replace("jobs.", "").Replace("careers.", "");

        // Known job boards - don't use their domain as company name
        var jobBoards = new[] { "linkedin.com", "indeed.com", "glassdoor.com", "monster.com", "ziprecruiter.com", "lever.co", "greenhouse.io" };
        if (jobBoards.Any(jb => host.Contains(jb)))
        {
            return null;
        }

        // Use domain as company name hint
        return host.Split('.')[0];
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static string CleanDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"\\n|\\r", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"[ \t]+", " ");

        return text.Trim();
    }

    private static string HtmlDecode(string text)
    {
        return System.Net.WebUtility.HtmlDecode(text)
            .Replace("\\n", "\n")
            .Replace("\\r", "")
            .Replace("\\t", " ")
            .Replace("\\\"", "\"");
    }
}




