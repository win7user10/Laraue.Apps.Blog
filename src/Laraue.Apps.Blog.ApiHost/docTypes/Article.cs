using Laraue.CmsBackend;

namespace Laraue.Apps.Blog.ApiHost.docTypes;

public class Article : BaseContentType
{
    public string[]? Projects { get; init; }
    public string[]? Tags { get; init; }
    public required string Description { get; init; }
    public string? NextLink { get; init; }
    public string? PreviousLink { get; init; }
}