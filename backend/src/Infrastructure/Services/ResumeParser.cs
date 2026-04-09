using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Joby.Application.DTOs.Profile;
using Joby.Application.Interfaces;
using UglyToad.PdfPig;

namespace Joby.Infrastructure.Services;

public class ResumeParser : IResumeParser
{
    // Curated skills list for matching
    private static readonly HashSet<string> KnownSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        // Programming Languages
        "Python", "JavaScript", "TypeScript", "Java", "C#", "C++", "C", "Go", "Rust", "Ruby",
        "PHP", "Swift", "Kotlin", "Scala", "R", "MATLAB", "Perl", "Haskell", "Elixir", "Clojure",
        
        // Frontend
        "React", "Angular", "Vue.js", "Vue", "Svelte", "Next.js", "Nuxt.js", "HTML", "CSS", "SASS",
        "SCSS", "Tailwind CSS", "Bootstrap", "Material UI", "jQuery", "Redux", "MobX", "Webpack",
        
        // Backend
        "Node.js", "Express.js", "Express", "Django", "Flask", "FastAPI", "Spring Boot", "Spring",
        ".NET", "ASP.NET", "Rails", "Ruby on Rails", "Laravel", "Symfony", "NestJS", "Koa",
        
        // Databases
        "SQL", "PostgreSQL", "MySQL", "SQLite", "MongoDB", "Redis", "Elasticsearch", "Cassandra",
        "DynamoDB", "Firebase", "Oracle", "SQL Server", "MariaDB", "Neo4j", "GraphQL",
        
        // Cloud & DevOps
        "AWS", "Azure", "GCP", "Google Cloud", "Docker", "Kubernetes", "Terraform", "Ansible",
        "Jenkins", "GitLab CI", "GitHub Actions", "CircleCI", "Travis CI", "Nginx", "Apache",
        
        // Data & ML
        "Machine Learning", "Deep Learning", "TensorFlow", "PyTorch", "Keras", "Scikit-learn",
        "Pandas", "NumPy", "Data Science", "Data Analysis", "Big Data", "Spark", "Hadoop",
        "Tableau", "Power BI", "NLP", "Computer Vision",
        
        // Tools & Practices
        "Git", "GitHub", "GitLab", "Bitbucket", "Jira", "Confluence", "Agile", "Scrum",
        "CI/CD", "DevOps", "REST API", "Microservices", "Linux", "Unix", "Windows Server",
        
        // Mobile
        "iOS", "Android", "React Native", "Flutter", "Xamarin", "SwiftUI", "Jetpack Compose"
    };

    public async Task<(string text, ParsedResumeData data)> ParseAsync(Stream fileStream, string fileName, string contentType)
    {
        string text;

        if (contentType == "application/pdf" || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            text = await ExtractTextFromPdfAsync(fileStream);
        }
        else if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                 fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            text = await ExtractTextFromDocxAsync(fileStream);
        }
        else
        {
            throw new NotSupportedException($"Unsupported file type: {contentType}");
        }

        var parsedData = ParseResumeText(text);
        return (text, parsedData);
    }

    private static async Task<string> ExtractTextFromPdfAsync(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var sb = new StringBuilder();

        using var document = PdfDocument.Open(memoryStream);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private static Task<string> ExtractTextFromDocxAsync(Stream fileStream)
    {
        using var document = WordprocessingDocument.Open(fileStream, false);
        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
        {
            return Task.FromResult(string.Empty);
        }

        var text = body.InnerText;
        return Task.FromResult(text);
    }

    private static ParsedResumeData ParseResumeText(string text)
    {
        var data = new ParsedResumeData();

        // Extract email
        var emailMatch = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        if (emailMatch.Success)
        {
            data.Email = emailMatch.Value;
        }

        // Extract phone
        var phoneMatch = Regex.Match(text, @"(\+?1?[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
        if (phoneMatch.Success)
        {
            data.Phone = phoneMatch.Value;
        }

        // Extract name (heuristic: first line or near email)
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count > 0)
        {
            // First non-empty line that looks like a name (2-4 words, no special characters)
            var nameLine = lines.FirstOrDefault(l =>
                Regex.IsMatch(l, @"^[A-Z][a-z]+(\s+[A-Z][a-z]+){1,3}$") &&
                !l.Contains("@") && !l.Contains("http"));

            data.Name = nameLine;
        }

        // Extract skills
        data.Skills = KnownSkills
            .Where(skill => Regex.IsMatch(text, $@"\b{Regex.Escape(skill)}\b", RegexOptions.IgnoreCase))
            .ToList();

        // Extract job titles (common patterns)
        var titlePatterns = new[]
        {
            @"(?:Senior|Junior|Lead|Principal|Staff|Associate)?\s*(?:Software|Full[- ]?Stack|Front[- ]?End|Back[- ]?End|Data|DevOps|Cloud|ML|Machine Learning|Mobile|iOS|Android|Web|QA|Test)?\s*(?:Engineer|Developer|Architect|Scientist|Analyst|Manager|Director|Consultant)",
            @"(?:Project|Product|Program|Engineering|Technical)\s*Manager",
            @"(?:CTO|CEO|VP|Chief)\s*(?:Technology|Technical|Engineering)?(?:\s*Officer)?"
        };

        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in titlePatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var title = match.Value.Trim();
                if (!string.IsNullOrWhiteSpace(title) && title.Length > 5)
                {
                    titles.Add(title);
                }
            }
        }
        data.JobTitles = titles.Take(5).ToList();

        // Extract summary (look for "Summary", "Profile", "Objective" sections)
        var summaryMatch = Regex.Match(text,
            @"(?:Summary|Profile|Objective|About\s*Me)[:\s]*\n?(.*?)(?:\n\n|Experience|Education|Skills|Work)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (summaryMatch.Success && summaryMatch.Groups[1].Value.Length > 20)
        {
            data.Summary = summaryMatch.Groups[1].Value.Trim().Substring(0, Math.Min(500, summaryMatch.Groups[1].Value.Trim().Length));
        }

        // Basic work experience extraction
        var experienceSection = Regex.Match(text,
            @"(?:Experience|Employment|Work\s*History)[:\s]*\n?(.*?)(?:Education|Skills|Projects|Certifications|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (experienceSection.Success)
        {
            var expText = experienceSection.Groups[1].Value;
            var companyMatches = Regex.Matches(expText, @"(?:at|@|,)\s*([A-Z][A-Za-z\s&]+(?:Inc|LLC|Ltd|Corp|Company)?)", RegexOptions.IgnoreCase);

            foreach (Match match in companyMatches.Take(5))
            {
                data.WorkExperience.Add(new WorkExperienceDto
                {
                    Company = match.Groups[1].Value.Trim()
                });
            }
        }

        // Basic education extraction
        var educationSection = Regex.Match(text,
            @"Education[:\s]*\n?(.*?)(?:Experience|Skills|Projects|Certifications|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (educationSection.Success)
        {
            var eduText = educationSection.Groups[1].Value;
            var degreeMatches = Regex.Matches(eduText,
                @"(Bachelor|Master|Ph\.?D|B\.?S\.?|M\.?S\.?|B\.?A\.?|M\.?A\.?|MBA)[^\n]*",
                RegexOptions.IgnoreCase);

            foreach (Match match in degreeMatches.Take(3))
            {
                data.Education.Add(new EducationDto
                {
                    Degree = match.Value.Trim()
                });
            }
        }

        return data;
    }
}




