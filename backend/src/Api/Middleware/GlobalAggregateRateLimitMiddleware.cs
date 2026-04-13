using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Joby.Api.Middleware;

/// <summary>
/// Enforces rolling-window limits on total accepted requests for this API process (all clients combined).
/// </summary>
public sealed class GlobalAggregateRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GlobalAggregateRateLimitOptions _options;
    private readonly Queue<DateTimeOffset> _timestamps = new();
    private readonly object _lock = new();

    public GlobalAggregateRateLimitMiddleware(RequestDelegate next, IOptions<GlobalAggregateRateLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled
            || HttpMethods.IsOptions(context.Request.Method)
            || ShouldBypass(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (_options.MaxRequestsPer10Minutes <= 0 || _options.MaxRequestsPerHour <= 0)
        {
            await _next(context);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var cutoffHour = now.AddHours(-1);
        var cutoff10m = now.AddMinutes(-10);

        bool reject;
        lock (_lock)
        {
            while (_timestamps.Count > 0 && _timestamps.Peek() < cutoffHour)
                _timestamps.Dequeue();

            var inLastHour = _timestamps.Count;
            var inLast10m = 0;
            foreach (var t in _timestamps)
            {
                if (t >= cutoff10m)
                    inLast10m++;
            }

            reject = inLast10m >= _options.MaxRequestsPer10Minutes
                     || inLastHour >= _options.MaxRequestsPerHour;

            if (!reject)
                _timestamps.Enqueue(now);
        }

        if (reject)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "Too many requests (application-wide limit).",
                context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private bool ShouldBypass(PathString path)
    {
        foreach (var prefix in _options.BypassPathPrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                continue;

            var normalized = prefix.StartsWith('/') ? prefix : "/" + prefix;
            if (path.StartsWithSegments(normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
