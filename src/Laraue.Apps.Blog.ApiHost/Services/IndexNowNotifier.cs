using System.Net.Http.Json;
using Laraue.CmsBackend;
using Microsoft.Extensions.Options;

namespace Laraue.Apps.Blog.ApiHost.Services;

/// <summary>
/// Pushes the current set of published URLs to the IndexNow API (Bing, Yandex, and other
/// participating search engines) so they get crawled promptly instead of waiting for the
/// next scheduled sitemap recrawl. Runs once shortly after each deploy/restart, since that's
/// the only point at which this markdown-file-backed CMS actually picks up content changes.
/// </summary>
public class IndexNowNotifier(
    IHttpClientFactory httpClientFactory,
    ISitemapGenerator sitemapGenerator,
    IOptions<SiteOptions> siteOptions,
    ILogger<IndexNowNotifier> logger)
    : BackgroundService
{
    private const string IndexNowEndpoint = "https://api.indexnow.org/indexnow";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var key = siteOptions.Value.IndexNowKey;
        if (string.IsNullOrEmpty(key))
        {
            logger.LogInformation("IndexNow key is not configured, skipping submission.");
            return;
        }

        // Give the app a moment to finish starting up before doing any outbound work.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        try
        {
            var baseAddress = siteOptions.Value.SitemapBaseAddress.TrimEnd('/');
            var host = new Uri(baseAddress).Host;

            var urls = sitemapGenerator.GetItems()
                .Select(item => $"{baseAddress}/{item.Location}")
                .ToArray();

            if (urls.Length == 0)
            {
                return;
            }

            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(IndexNowEndpoint, new
            {
                host,
                key,
                keyLocation = $"{baseAddress}/{key}.txt",
                urlList = urls,
            }, stoppingToken);

            logger.LogInformation(
                "Submitted {Count} URLs to IndexNow, response status: {Status}",
                urls.Length,
                response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to submit URLs to IndexNow.");
        }
    }
}
