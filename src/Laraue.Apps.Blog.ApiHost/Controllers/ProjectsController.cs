using Laraue.CmsBackend;
using Laraue.Core.DataAccess.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Blog.ApiHost.Controllers;

[ApiController]
[Route("api/blog/projects")]
public class ProjectsController(ICmsBackend cmsBackend) : ControllerBase
{
    private static readonly string[] RootPath = ["blog", "projects"];

    [HttpGet]
    public IShortPaginatedResult<CardItem> GetProjects(
        [FromQuery] string languageCode,
        [FromQuery] int page = 0,
        [FromQuery] int perPage = 16)
    {
        return BlogQueryHelpers.GetList(cmsBackend, RootPath, languageCode, ["project"], null, page, perPage);
    }

    [HttpGet("{fileName}")]
    public CardDetail GetProject(
        [FromRoute] string fileName,
        [FromQuery] string languageCode)
    {
        return BlogQueryHelpers.GetDetails(cmsBackend, [..RootPath, fileName], languageCode);
    }
}
