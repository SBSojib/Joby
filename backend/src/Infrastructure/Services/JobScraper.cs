using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    public async Task<JobScrapeResult?> ScrapeJobAsync(string url)
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
                    return await FinishScrapeAsync(url, html, job);
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

            return await FinishScrapeAsync(url, html, job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping job from URL {Url}", url);
            return null;
        }
    }

    private bool TryParseJsonLd(string json, CreateJobRequest job)
    {
        if (!json.Contains("JobPosting", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TryApplyJobPostingFromJsonNode(json, job))
        {
            return true;
        }

        try
        {
            // Fallback: regex (does not decode JSON \uXXXX escapes in descriptions)
            if (json.Contains("\"@type\"", StringComparison.OrdinalIgnoreCase))
            {
                var titleMatch = Regex.Match(json, @"""title""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (titleMatch.Success) job.Title = HtmlDecode(titleMatch.Groups[1].Value);

                var companyMatch = Regex.Match(json, @"""hiringOrganization""[^}]*""name""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (companyMatch.Success) job.Company = HtmlDecode(companyMatch.Groups[1].Value);

                var locationMatch = Regex.Match(json, @"""jobLocation""[^}]*""addressLocality""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (locationMatch.Success) job.Location = HtmlDecode(locationMatch.Groups[1].Value);

                var descMatch = Regex.Match(json, @"""description""\s*:\s*""([^""]{10,})""", RegexOptions.IgnoreCase);
                if (descMatch.Success) job.Description = CleanDescription(HtmlDecode(descMatch.Groups[1].Value));

                var salaryMatch = Regex.Match(json, @"""baseSalary""[^}]*""value""\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (salaryMatch.Success) job.Salary = salaryMatch.Groups[1].Value;

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

    /// <summary>
    /// Parses JSON-LD with System.Text.Json so JSON string unicode escapes (\u003c) become real characters.
    /// </summary>
    private static bool TryApplyJobPostingFromJsonNode(string json, CreateJobRequest job)
    {
        try
        {
            var root = JsonNode.Parse(json.Trim());
            var jp = FindJobPostingObject(root);
            if (jp == null)
            {
                return false;
            }

            if (jp["title"] is JsonValue titleVal && titleVal.TryGetValue<string>(out var title) && !string.IsNullOrWhiteSpace(title))
            {
                job.Title = HtmlDecode(title);
            }

            if (jp["hiringOrganization"] is JsonObject org)
            {
                if (org["name"] is JsonValue nameVal && nameVal.TryGetValue<string>(out var orgName) && !string.IsNullOrWhiteSpace(orgName))
                {
                    job.Company = HtmlDecode(orgName);
                }
            }

            TryApplyJobLocationFromJsonNode(jp, job);

            if (jp.TryGetPropertyValue("description", out var descNode) && descNode != null)
            {
                var descText = ExtractDescriptionText(descNode);
                if (!string.IsNullOrWhiteSpace(descText))
                {
                    job.Description = CleanDescription(HtmlDecode(descText));
                }
            }

            if (jp.TryGetPropertyValue("baseSalary", out var salaryNode) && salaryNode != null)
            {
                var amount = FindFirstDecimalString(salaryNode);
                if (!string.IsNullOrWhiteSpace(amount))
                {
                    job.Salary = amount;
                }
            }

            if (jp.TryGetPropertyValue("employmentType", out var empNode) && empNode != null)
            {
                var et = EmploymentTypeAsString(empNode);
                if (!string.IsNullOrWhiteSpace(et))
                {
                    job.JobType = HtmlDecode(et);
                }
            }

            return !string.IsNullOrWhiteSpace(job.Title);
        }
        catch
        {
            return false;
        }
    }

    private static JsonObject? FindJobPostingObject(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj when IsJobPostingType(obj):
                return obj;
            case JsonObject obj:
            {
                foreach (var prop in obj)
                {
                    var found = FindJobPostingObject(prop.Value);
                    if (found != null)
                    {
                        return found;
                    }
                }

                break;
            }
            case JsonArray arr:
            {
                foreach (var item in arr)
                {
                    var found = FindJobPostingObject(item);
                    if (found != null)
                    {
                        return found;
                    }
                }

                break;
            }
        }

        return null;
    }

    private static bool IsJobPostingType(JsonObject obj)
    {
        if (!obj.TryGetPropertyValue("@type", out var t) || t == null)
        {
            return false;
        }

        if (t is JsonValue v && v.TryGetValue<string>(out var s))
        {
            return string.Equals(s, "JobPosting", StringComparison.OrdinalIgnoreCase);
        }

        if (t is JsonArray a)
        {
            return a.Any(x => x is JsonValue jv && jv.TryGetValue<string>(out var ts) && string.Equals(ts, "JobPosting", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private static void TryApplyJobLocationFromJsonNode(JsonObject jp, CreateJobRequest job)
    {
        if (!string.IsNullOrWhiteSpace(job.Location))
        {
            return;
        }

        if (!jp.TryGetPropertyValue("jobLocation", out var locNode) || locNode == null)
        {
            return;
        }

        if (locNode is JsonArray locArr && locArr.Count > 0)
        {
            locNode = locArr[0];
        }

        if (locNode is not JsonObject place)
        {
            return;
        }

        if (place["address"] is JsonObject addr)
        {
            if (addr["addressLocality"] is JsonValue alv && alv.TryGetValue<string>(out var locality))
            {
                job.Location = HtmlDecode(locality);
                return;
            }
        }

        if (place["addressLocality"] is JsonValue v2 && v2.TryGetValue<string>(out var loc2))
        {
            job.Location = HtmlDecode(loc2);
        }
    }

    private static string? ExtractDescriptionText(JsonNode descNode)
    {
        if (descNode is JsonValue jv && jv.TryGetValue<string>(out var s))
        {
            return s;
        }

        if (descNode is JsonObject dObj)
        {
            if (dObj["text"] is JsonValue tv && tv.TryGetValue<string>(out var text))
            {
                return text;
            }

            if (dObj["@value"] is JsonValue vv && vv.TryGetValue<string>(out var vval))
            {
                return vval;
            }
        }

        return null;
    }

    private static string? FindFirstDecimalString(JsonNode node)
    {
        if (node is JsonValue jv)
        {
            if (jv.TryGetValue<decimal>(out var dec))
            {
                return dec.ToString("0");
            }

            if (jv.TryGetValue<string>(out var str) && decimal.TryParse(str, out var d2))
            {
                return d2.ToString("0");
            }
        }

        if (node is JsonObject o)
        {
            foreach (var p in new[] { "value", "minValue", "maxValue" })
            {
                if (o[p] is JsonValue v && v.TryGetValue<decimal>(out var dec))
                {
                    return dec.ToString("0");
                }
            }

            foreach (var prop in o)
            {
                var inner = FindFirstDecimalString(prop.Value!);
                if (inner != null)
                {
                    return inner;
                }
            }
        }

        if (node is JsonArray a)
        {
            foreach (var item in a)
            {
                var inner = FindFirstDecimalString(item!);
                if (inner != null)
                {
                    return inner;
                }
            }
        }

        return null;
    }

    private static string? EmploymentTypeAsString(JsonNode node)
    {
        if (node is JsonValue jv && jv.TryGetValue<string>(out var s))
        {
            return s;
        }

        if (node is JsonArray a && a.Count > 0 && a[0] is JsonValue j0 && j0.TryGetValue<string>(out var first))
        {
            return first;
        }

        return null;
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

    private async Task<JobScrapeResult> FinishScrapeAsync(string url, string rawHtml, CreateJobRequest job)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var pageUri))
        {
            return new JobScrapeResult { Metadata = job, RawPageHtml = rawHtml };
        }

        // Pinpoint: the HTTP body is the same as curl — but list bodies live in JSON `htmlString` next to empty
        // divs; scripts do not run inside our iframe. Expand those fragments from the same response first.
        if (pageUri.Host.Contains("pinpointhq.com", StringComparison.OrdinalIgnoreCase))
        {
            var fromSameResponse = TryBuildPinpointStaticDocumentFromFetchedHtml(rawHtml, pageUri);
            if (!string.IsNullOrEmpty(fromSameResponse))
            {
                return new JobScrapeResult { Metadata = job, RawPageHtml = fromSameResponse };
            }

            var fromPostingsApi = await TryGetPinpointSynthesizedListingAsync(url, job);
            if (!string.IsNullOrEmpty(fromPostingsApi))
            {
                return new JobScrapeResult { Metadata = job, RawPageHtml = fromPostingsApi };
            }
        }

        return new JobScrapeResult { Metadata = job, RawPageHtml = rawHtml };
    }

    /// <summary>
    /// Builds a static HTML document from the Pinpoint job page HTML alone (one GET, same as curl).
    /// Pulls <c>htmlString</c> payloads from <c>Renderrichtrixcontent</c> React-on-Rails script tags and visible "About" markup.
    /// </summary>
    private static string? TryBuildPinpointStaticDocumentFromFetchedHtml(string rawHtml, Uri pageUri)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            var scripts = doc.DocumentNode.SelectNodes(
                "//script[@type='application/json' and contains(concat(' ', normalize-space(@class), ' '), ' js-react-on-rails-component ')]");
            if (scripts == null)
            {
                return null;
            }

            var blocks = new List<(string? Heading, string Html)>();
            foreach (var script in scripts)
            {
                var component = script.GetAttributeValue("data-component-name", string.Empty);
                if (!component.Contains("Renderrichtrixcontent", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var inner = script.InnerText;
                if (string.IsNullOrWhiteSpace(inner))
                {
                    continue;
                }

                using var parsed = JsonDocument.Parse(inner);
                if (!parsed.RootElement.TryGetProperty("htmlString", out var hs) || hs.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var fragment = hs.GetString();
                if (string.IsNullOrWhiteSpace(fragment))
                {
                    continue;
                }

                var headingNode = script.SelectSingleNode("preceding::h2[contains(@class,'external-panel__heading')][1]");
                var heading = string.IsNullOrWhiteSpace(headingNode?.InnerText)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(headingNode.InnerText.Trim());
                blocks.Add((heading, fragment));
            }

            if (blocks.Count == 0)
            {
                return null;
            }

            var title = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'external-panel__title')]")?.InnerText?.Trim()
                        ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim()
                        ?? "Job";

            var metaBlock = doc.DocumentNode.SelectSingleNode("//dl[@id='external-jobs-show-meta-mobile']")?.OuterHtml
                            ?? doc.DocumentNode.SelectSingleNode("//sidebar//dl[contains(@class,'external-definition-list')]")?.OuterHtml;

            var aboutHeading = doc.DocumentNode.SelectSingleNode("//h2[@id='about-heading']")?.InnerText?.Trim();
            var aboutInner = doc.DocumentNode.SelectSingleNode("//div[@id='about-body']")?.InnerHtml;

            var sb = new StringBuilder();
            AppendPinpointStaticDocumentHead(sb, pageUri, title);

            if (!string.IsNullOrWhiteSpace(metaBlock))
            {
                sb.Append("<section class=\"jd-section jd-meta\">");
                sb.Append("<div class=\"pinpoint-meta\">").Append(metaBlock).Append("</div>");
                sb.Append("</section>");
            }

            foreach (var (heading, html) in blocks)
            {
                sb.Append("<section class=\"jd-section\">");
                if (!string.IsNullOrWhiteSpace(heading))
                {
                    sb.Append("<h2 class=\"pinpoint-section-title\">")
                        .Append(System.Net.WebUtility.HtmlEncode(heading))
                        .Append("</h2>");
                }

                sb.Append("<div class=\"section-body\">").Append(html).Append("</div>");
                sb.Append("</section>");
            }

            if (!string.IsNullOrWhiteSpace(aboutInner))
            {
                sb.Append("<section class=\"jd-section\">");
                sb.Append("<h2 class=\"pinpoint-section-title\">")
                    .Append(System.Net.WebUtility.HtmlEncode(aboutHeading ?? "About"))
                    .Append("</h2>");
                sb.Append("<div class=\"section-body\">").Append(aboutInner).Append("</div>");
                sb.Append("</section>");
            }

            TryAppendPinpointHiringStagesFromPage(doc, sb);

            sb.Append("</body></html>");
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void AppendPinpointStaticDocumentHead(StringBuilder sb, Uri pageUri, string pageTitle)
    {
        var baseHref = $"{pageUri.Scheme}://{pageUri.Authority}/";
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<base target=\"_blank\" href=\"")
            .Append(System.Net.WebUtility.HtmlEncode(baseHref))
            .Append("\">");
        sb.Append("<style>");
        sb.Append("body{font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;line-height:1.55;");
        sb.Append("max-width:720px;margin:0 auto;padding:1.5rem 1.25rem 2.5rem;color:#1a1a1a;background:#fafafa;}");
        sb.Append("h1{font-size:1.6rem;font-weight:700;margin:0 0 1.25rem;line-height:1.25;color:#111;}");
        sb.Append(".jd-section{margin:0 0 1rem;padding:1rem 1.15rem;background:#fff;border:1px solid #e5e5e5;border-radius:10px;box-shadow:0 1px 2px rgba(0,0,0,.04);}");
        sb.Append(".jd-section.jd-meta{padding:0;border:none;background:transparent;box-shadow:none;margin-bottom:1.25rem;}");
        sb.Append(".pinpoint-meta{padding:1rem 1.1rem;background:#f0f0f0;border-radius:10px;border:1px solid #e8e8e8;font-size:.95rem;}");
        sb.Append(".pinpoint-meta dl{margin:0;}");
        sb.Append(".pinpoint-meta dt{font-weight:600;color:#a04538;margin-top:.5rem;}");
        sb.Append(".pinpoint-meta dt:first-child{margin-top:0;}");
        sb.Append(".pinpoint-meta dd{margin:0 0 .35rem 0;padding-left:0;}");
        sb.Append("h2.pinpoint-section-title{font-size:1.05rem;font-weight:700;margin:0 0 .6rem;color:#1a1a1a;}");
        sb.Append(".section-body{font-size:1rem;color:#333;}");
        sb.Append(".section-body ul{padding-left:1.35rem;margin:.35rem 0 0;}");
        sb.Append(".section-body li{margin:.35rem 0;}");
        sb.Append(".section-body p{margin:.5rem 0;}");
        sb.Append(".jd-ol{margin:.35rem 0 0;padding-left:1.35rem;}");
        sb.Append(".jd-ol li{margin:.35rem 0;}");
        sb.Append("</style></head><body>");
        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(pageTitle)).Append("</h1>");
    }

    /// <summary>
    /// Hiring pipeline stages from React-on-Rails JSON (same page as curl); not in htmlString blocks.
    /// </summary>
    private static void TryAppendPinpointHiringStagesFromPage(HtmlDocument doc, StringBuilder sb)
    {
        var script = doc.DocumentNode.SelectSingleNode(
            "//script[@type='application/json' and contains(concat(' ', normalize-space(@class), ' '), ' js-react-on-rails-component ') and contains(@data-component-name,'Stagescarousel')]");
        if (script == null)
        {
            return;
        }

        try
        {
            using var parsed = JsonDocument.Parse(script.InnerText);
            var root = parsed.RootElement;
            if (!root.TryGetProperty("stages", out var stages) || stages.ValueKind != JsonValueKind.Array || stages.GetArrayLength() == 0)
            {
                return;
            }

            var sectionTitle = root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()
                : "Hiring process";

            sb.Append("<section class=\"jd-section\"><h2 class=\"pinpoint-section-title\">")
                .Append(System.Net.WebUtility.HtmlEncode(sectionTitle ?? "Hiring process"))
                .Append("</h2><ol class=\"jd-ol\">");

            foreach (var st in stages.EnumerateArray())
            {
                var name = st.TryGetProperty("externalName", out var n) && n.ValueKind == JsonValueKind.String
                    ? n.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    sb.Append("<li>").Append(System.Net.WebUtility.HtmlEncode(name)).Append("</li>");
                }
            }

            sb.Append("</ol></section>");
        }
        catch
        {
            // ignore malformed optional block
        }
    }

    /// <summary>
    /// Pinpoint external careers pages are React shells: lists live in <c>/postings.json</c>, not the initial HTML.
    /// When we can match the listing, build a self-contained HTML document for iframe display.
    /// </summary>
    private async Task<string?> TryGetPinpointSynthesizedListingAsync(string jobPageUrl, CreateJobRequest job)
    {
        if (!Uri.TryCreate(jobPageUrl, UriKind.Absolute, out var listingUri))
        {
            return null;
        }

        if (!listingUri.Host.Contains("pinpointhq.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var jsonUrl = $"{listingUri.Scheme}://{listingUri.Authority}/postings.json";
            using var req = new HttpRequestMessage(HttpMethod.Get, jsonUrl);
            req.Headers.TryAddWithoutValidation("Accept", "application/json, */*;q=0.8");

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pinpoint postings.json failed for {JsonUrl}: {Status}", jsonUrl, response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            JsonElement? matched = null;
            foreach (var item in data.EnumerateArray())
            {
                var postingUrl = GetJsonString(item, "url");
                if (PinpointJobUrlsMatch(postingUrl, jobPageUrl))
                {
                    matched = item;
                    break;
                }
            }

            if (matched is not { } posting)
            {
                return null;
            }

            ApplyPinpointPostingMetadata(posting, job);
            return BuildPinpointListingDocument(posting, listingUri);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Pinpoint enrichment skipped for {Url}", jobPageUrl);
            return null;
        }
    }

    private static bool PinpointJobUrlsMatch(string? postingUrl, string requestedUrl)
    {
        if (string.IsNullOrEmpty(postingUrl))
        {
            return false;
        }

        if (string.Equals(postingUrl.TrimEnd('/'), requestedUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(postingUrl, UriKind.Absolute, out var a) || !Uri.TryCreate(requestedUrl, UriKind.Absolute, out var b))
        {
            return false;
        }

        return string.Equals(
            a.PathAndQuery.TrimEnd('/'),
            b.PathAndQuery.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyPinpointPostingMetadata(JsonElement posting, CreateJobRequest job)
    {
        var title = GetJsonString(posting, "title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            job.Title = title;
        }

        var location = GetPinpointLocationDisplay(posting);
        if (!string.IsNullOrWhiteSpace(location))
        {
            job.Location = location;
        }

        var compensation = GetJsonString(posting, "compensation");
        if (!string.IsNullOrWhiteSpace(compensation))
        {
            job.Salary = compensation;
        }

        var employment = GetJsonString(posting, "employment_type_text");
        if (!string.IsNullOrWhiteSpace(employment))
        {
            job.JobType = employment;
        }

        var workplace = GetJsonString(posting, "workplace_type_text");
        if (!string.IsNullOrWhiteSpace(workplace))
        {
            if (!string.IsNullOrWhiteSpace(job.JobType))
            {
                job.JobType = $"{job.JobType} · {workplace}";
            }
            else
            {
                job.JobType = workplace;
            }
        }
    }

    private static string? GetPinpointDepartmentName(JsonElement posting)
    {
        if (!posting.TryGetProperty("job", out var jobEl) || jobEl.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!jobEl.TryGetProperty("department", out var dept) || dept.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetJsonString(dept, "name");
    }

    private static string? GetPinpointLocationDisplay(JsonElement posting)
    {
        if (!posting.TryGetProperty("location", out var loc) || loc.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = GetJsonString(loc, "name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return GetJsonString(loc, "city");
    }

    private static string BuildPinpointListingDocument(JsonElement posting, Uri careersSiteOrigin)
    {
        var title = GetJsonString(posting, "title") ?? "Job posting";
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        var baseHref = $"{careersSiteOrigin.Scheme}://{careersSiteOrigin.Authority}/";
        sb.Append("<base target=\"_blank\" href=\"")
            .Append(System.Net.WebUtility.HtmlEncode(baseHref))
            .Append("\">");
        sb.Append("<style>");
        sb.Append("body{font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;line-height:1.55;");
        sb.Append("max-width:720px;margin:0 auto;padding:1.5rem 1.25rem 2.5rem;color:#1a1a1a;background:#fafafa;}");
        sb.Append("h1{font-size:1.6rem;font-weight:700;margin:0 0 1.25rem;line-height:1.25;color:#111;}");
        sb.Append(".jd-section{margin:0 0 1rem;padding:1rem 1.15rem;background:#fff;border:1px solid #e5e5e5;border-radius:10px;box-shadow:0 1px 2px rgba(0,0,0,.04);}");
        sb.Append(".jd-section.jd-meta{padding:0;border:none;background:transparent;box-shadow:none;margin-bottom:1.25rem;}");
        sb.Append(".meta{padding:1rem 1.1rem;background:#f0f0f0;border-radius:10px;border:1px solid #e8e8e8;font-size:.95rem;}");
        sb.Append(".meta-row{display:flex;flex-wrap:wrap;gap:.25rem .75rem;}");
        sb.Append(".meta-label{font-weight:600;color:#a04538;}");
        sb.Append("h2{font-size:1.05rem;font-weight:700;margin:0 0 .6rem;color:#1a1a1a;}");
        sb.Append(".section-body{font-size:1rem;color:#333;}");
        sb.Append(".section-body ul{padding-left:1.35rem;margin:.35rem 0 0;}");
        sb.Append(".section-body li{margin:.35rem 0;}");
        sb.Append(".section-body p{margin:.5rem 0;}");
        sb.Append("</style></head><body>");

        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(title)).Append("</h1>");

        sb.Append("<section class=\"jd-section jd-meta\"><div class=\"meta\">");
        AppendMetaRow(sb, "Compensation", GetJsonString(posting, "compensation"));
        AppendMetaRow(sb, "Employment type", GetJsonString(posting, "employment_type_text"));
        AppendMetaRow(sb, "Workplace type", GetJsonString(posting, "workplace_type_text"));
        AppendMetaRow(sb, "Location", GetPinpointLocationDisplay(posting));
        AppendMetaRow(sb, "Department", GetPinpointDepartmentName(posting));
        sb.Append("</div></section>");

        AppendHtmlSectionInCard(sb, null, GetJsonString(posting, "description"));
        AppendHtmlSectionInCard(sb, GetJsonString(posting, "key_responsibilities_header"), GetJsonString(posting, "key_responsibilities"));
        AppendHtmlSectionInCard(sb, GetJsonString(posting, "skills_knowledge_expertise_header"), GetJsonString(posting, "skills_knowledge_expertise"));
        AppendHtmlSectionInCard(sb, GetJsonString(posting, "benefits_header"), GetJsonString(posting, "benefits"));

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void AppendMetaRow(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.Append("<div class=\"meta-row\"><span class=\"meta-label\">")
            .Append(System.Net.WebUtility.HtmlEncode(label))
            .Append(":</span><span>")
            .Append(System.Net.WebUtility.HtmlEncode(value))
            .Append("</span></div>");
    }

    private static void AppendHtmlSectionInCard(StringBuilder sb, string? header, string? htmlFragment)
    {
        if (string.IsNullOrWhiteSpace(htmlFragment))
        {
            return;
        }

        sb.Append("<section class=\"jd-section\">");
        if (!string.IsNullOrWhiteSpace(header))
        {
            sb.Append("<h2>").Append(System.Net.WebUtility.HtmlEncode(header)).Append("</h2>");
        }

        sb.Append("<div class=\"section-body\">").Append(htmlFragment).Append("</div>");
        sb.Append("</section>");
    }

    private static string? GetJsonString(JsonElement el, string propertyName)
    {
        if (!el.TryGetProperty(propertyName, out var p))
        {
            return null;
        }

        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number => p.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}




