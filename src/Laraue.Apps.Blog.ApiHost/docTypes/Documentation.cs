using Laraue.CmsBackend;

namespace Laraue.Apps.Blog.ApiHost.docTypes;

public class Documentation : BaseContentType
{
    public required string Project { get; set; }
    public string? Description { get; set; }
    public string[]? Keywords { get; set; }
}