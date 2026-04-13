namespace Joby.Api.Middleware;

/// <summary>
/// Application-wide (aggregate) limits: all clients share one counter in this process.
/// </summary>
public sealed class GlobalAggregateRateLimitOptions
{
    public const string SectionName = "GlobalRateLimit";

    public bool Enabled { get; set; } = true;

    public int MaxRequestsPer10Minutes { get; set; } = 20;

    public int MaxRequestsPerHour { get; set; } = 100;

    /// <summary>
    /// Paths that do not count toward the limit (e.g. Docker/K8s liveness).
    /// </summary>
    public string[] BypassPathPrefixes { get; set; } = ["/health/live", "/health/ready"];
}
