using Laraue.CmsBackend;

namespace Laraue.Apps.Blog.ApiHost.docTypes;

public class SectionDefinition : BaseContentType
{
    public string? Description { get; set; }
    public string[]? Keywords { get; set; }
    public required int Order { get; set; }
}