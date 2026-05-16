using System.Net;
using Joby.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Joby.Tests;

public class JobScraperTests
{
    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("file:///etc/passwd")]
    public async Task ScrapeJobAsync_WithUnsafeUrl_DoesNotSendRequest(string url)
    {
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var scraper = CreateScraper(handler);

        var result = await scraper.ScrapeJobAsync(url);

        Assert.Null(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ScrapeJobAsync_WithRedirectToPrivateAddress_DoesNotFollowRedirect()
    {
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers =
            {
                Location = new Uri("http://127.0.0.1/admin")
            }
        });
        var scraper = CreateScraper(handler);

        var result = await scraper.ScrapeJobAsync("https://93.184.216.34/jobs/1");

        Assert.Null(result);
        Assert.Equal(1, handler.RequestCount);
    }

    private static JobScraper CreateScraper(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        return new JobScraper(httpClient, Mock.Of<ILogger<JobScraper>>());
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CountingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
