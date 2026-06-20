using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Laraue.Apps.Blog.ApiHost.Services;

public interface IRssFeedGenerator
{
    string Generate(string languageCode, IList<RssFeedItem> items);
}

public class RssFeedGenerator : IRssFeedGenerator
{
    private readonly string _baseUrl;

    private readonly Dictionary<string, string> _descriptions = new()
    {
        ["en"] = "Articles on software engineering, architecture, and indie building.",
        ["ru"] = "Статьи о создании ПО, архитектуре и соло-разработке",
    };

    public RssFeedGenerator(IOptions<SiteOptions> siteOptions)
    {
        _baseUrl = siteOptions.Value.SitemapBaseAddress.TrimEnd('/');
    }

    public string Generate(string languageCode, IList<RssFeedItem> items)
    {
        var feedUrl = $"{_baseUrl}/blog";
        var selfUrl = $"{_baseUrl}/api/rss?languageCode={languageCode}";

        var feed = new SyndicationFeed(
            title: "Laraue Blog",
            description: _descriptions.GetValueOrDefault(languageCode),
            feedAlternateLink: new Uri(feedUrl))
        {
            Language = languageCode,
            LastUpdatedTime = items.Count > 0
                ? items[0].UpdatedAt
                : DateTimeOffset.UtcNow,
        };

        feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(selfUrl), "application/rss+xml"));

        feed.Items = items.Select(card =>
        {
            var articleUrl = $"{_baseUrl}/{string.Join("/", card.Path)}";

            var item = new SyndicationItem(
                title: card.Title,
                content: card.Content,
                itemAlternateLink: new Uri(articleUrl),
                id: articleUrl,
                lastUpdatedTime: card.UpdatedAt)
            {
                PublishDate = card.CreatedAt,
                Summary = new TextSyndicationContent(card.Description),
            };

            foreach (var tag in card.Tags ?? [])
                item.Categories.Add(new SyndicationCategory(tag));

            return item;
        });

        return SerializeToRss2(feed);
    }

    private static string SerializeToRss2(SyndicationFeed feed)
    {
        var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

        using (var writer = XmlWriter.Create(ms, settings))
        {
            var rssWriter = new Rss20FeedFormatter(feed);
            rssWriter.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}

public class RssFeedItem
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Description { get; set; }
    public required string[] Path { get; set; }
    public required string[]? Tags { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required DateTime CreatedAt { get; set; }
}