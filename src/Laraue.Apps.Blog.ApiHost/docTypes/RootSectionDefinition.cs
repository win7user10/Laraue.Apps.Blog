using Laraue.CmsBackend;

namespace Laraue.Apps.Blog.ApiHost.docTypes;

public class RootSectionDefinition : BaseContentType
{
    public string? Icon { get; init; }
    public string? Description { get; init; }
}