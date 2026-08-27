using Laraue.CmsBackend;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Blog.ApiHost.Controllers;

[ApiController]
[Route("api/blog")]
public class DocumentationController(ICmsBackend cmsBackend) : ControllerBase
{
    private static readonly string[] RootPath = ["blog", "documentation"];

    [HttpGet("documentation")]
    [HttpGet("documentation/{**path}")]
    public CardDetail GetDocumentation(
        string? path,
        [FromQuery] string languageCode)
    {
        return BlogQueryHelpers.GetDetails(cmsBackend, BlogQueryHelpers.CombinePath(RootPath, path), languageCode);
    }

    [HttpGet("documentation-section")]
    [HttpGet("documentation-section/{**path}")]
    public CardMeta GetDocumentationSection(
        string? path,
        [FromQuery] string languageCode)
    {
        return BlogQueryHelpers.GetSectionMeta(cmsBackend, BlogQueryHelpers.CombinePath(RootPath, path), languageCode);
    }

    [HttpGet("documentation-menu")]
    public DocsMenuSection[] GetDocumentationMenu(
        [FromQuery] string languageCode,
        [FromQuery] string[] fromPath)
    {
        return BlogQueryHelpers.GetMenu(cmsBackend, fromPath, languageCode);
    }

    [HttpGet("documentation-tree")]
    public List<ManuItem> GetDocumentationTree(
        [FromQuery] string languageCode)
    {
        return BlogQueryHelpers.GetTree(cmsBackend, RootPath, languageCode, int.MaxValue);
    }
}
