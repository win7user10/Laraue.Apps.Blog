using Laraue.Apps.Blog.ApiHost.Services;
using Laraue.CmsBackend;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Blog.ApiHost.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController(ICmsBackend cmsBackend) : ControllerBase
{
    [HttpGet("og-image")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult GetOgImage([FromQuery] string[] articlePath, [FromQuery] string languageCode)
    {
        var entity = cmsBackend
            .GetEntity<OgImageData>(new GetEntityRequest
            {
                Path = articlePath,
                LanguageCode = languageCode,
                Properties = [
                    "title",
                    "description"
                ]
            });
        
        var pngBytes = OgImageGenerator.Generate(
            siteName: "Laraue Blog",
            title: entity.Title,
            description: entity.Description);
 
        return File(pngBytes, "image/png");
    }

    public class OgImageData
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
    }
}