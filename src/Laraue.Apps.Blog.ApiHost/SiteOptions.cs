namespace Laraue.Apps.Blog.ApiHost;

public record SiteOptions
{
    public required string SitemapBaseAddress { get; set; }
    public string? IndexNowKey { get; set; }
}