using Laraue.CmsBackend;
using Laraue.Core.DataAccess.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Blog.ApiHost.Controllers;

[ApiController]
[Route("api/blog/articles")]
public class ArticlesController(ICmsBackend cmsBackend) : ControllerBase
{
    private static readonly string[] RootPath = ["blog", "articles"];

    [HttpGet]
    public IShortPaginatedResult<CardItem> GetArticles(
        [FromQuery] string languageCode,
        [FromQuery] string? tag,
        [FromQuery] int page = 0,
        [FromQuery] int perPage = 16)
    {
        return BlogQueryHelpers.GetList(cmsBackend, RootPath, languageCode, ["article"], tag, page, perPage);
    }

    [HttpGet("{fileName}")]
    public CardDetail GetArticle(
        [FromRoute] string fileName,
        [FromQuery] string languageCode)
    {
        return BlogQueryHelpers.GetDetails(cmsBackend, [..RootPath, fileName], languageCode);
    }
}
