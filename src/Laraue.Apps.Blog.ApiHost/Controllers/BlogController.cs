using Laraue.CmsBackend;
using Laraue.CmsBackend.Contracts;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Interpreter.Markdown;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Blog.ApiHost.Controllers;

[ApiController]
[Route("api/blog")]
public class BlogController(ICmsBackend cmsBackend) : ControllerBase
{
    [HttpPost("list")]
    public IShortPaginatedResult<CardItem> Get([FromBody] GetCardsRequest request)
    {
        var filters = new List<FilterRow>();
        if (request.ContentTypes.Length > 0)
        {
            filters.Add(new FilterRow
            {
                Property = "contentType",
                Operator = FilterOperator.ValueInList,
                Value = request.ContentTypes,
            });
        }

        if (!string.IsNullOrEmpty(request.Tag))
        {
            filters.Add(new FilterRow
            {
                Property = "tags",
                Operator = FilterOperator.ValueListContain,
                Value = request.Tag,
            });
        }
        
        return cmsBackend.GetEntities<CardItem>(new GetEntitiesRequest
        {
            FromPath = request.Path,
            LanguageCode = request.LanguageCode,
            Properties = ["fileName", "title", "description", "contentType", "path", "length(content)", "tags", "projects"],
            Pagination = request.Pagination,
            Filters = filters.ToArray()
        });
    }
    
    [HttpGet("categories")]
    public List<ManuItem> GetCategories([FromQuery] string languageCode)
    {
        var rows = cmsBackend
            .GetSections(new GetSectionsRequest
            {
                LanguageCode = languageCode,
                Depth = 2,
                FromPath = ["blog"],
            })
            .Where(x => x.FileName != "documentation")
            .Where(x => x.FileName != "undefined")
            .ToList();
        
        var mainSection = cmsBackend
            .GetSections(new GetSectionsRequest
            {
                LanguageCode = languageCode,
                Depth = 1,
            })
            .First();

        mainSection.Children = rows.ToArray();
        
        rows.Insert(0, mainSection);
        
        var all = rows
            .Select(Map)
            .ToList();
        
        return all;
    }
    
    [HttpGet("docs")]
    public List<ManuItem> GetDocs([FromQuery] string languageCode)
    {
        var rows = cmsBackend
            .GetSections(new GetSectionsRequest
            {
                LanguageCode = languageCode,
                Depth = int.MaxValue,
                FromPath = ["blog", "documentation"],
            });
        
        var all = rows
            .Select(Map)
            .ToList();
        
        return all;
    }
    
    [HttpGet("docs-hierarchy")]
    public DocsMenuSection[] GetDocs([FromQuery] string languageCode, [FromQuery] string[] fromPath)
    {
        var rows = cmsBackend
            .GetSections(new GetSectionsRequest
            {
                LanguageCode = languageCode,
                Depth = 2,
                FromPath = fromPath,
            });

        var result = new List<DocsMenuSection>();
        foreach (var row in rows)
        {
            var childrenResult = new List<DocsMenuItem>();
            foreach (var child in row.Children)
            {
                childrenResult.Add(new DocsMenuItem
                {
                    Title = child.Title,
                    Path = child.FullPath,
                });
            }
            
            result.Add(new DocsMenuSection
            {
                Children = childrenResult.ToArray(),
                Title = row.Title,
                Path = row.FullPath,
            });
        }
        
        return result.ToArray();
    }
    
    [HttpPost("details")]
    public CardDetail GetDoc([FromBody] GetCardRequest request)
    {
        var all = cmsBackend.GetEntities<NeighborCard>(new GetEntitiesRequest
        {
            LanguageCode = request.LanguageCode,
            Pagination = new PaginationData
            {
                Page = 0,
                PerPage = 10000,
            },
            Properties = ["path", "title"],
            FromPath = ["blog"]
        });
        
        var result = cmsBackend
            .GetEntity<CardDetail>(new GetEntityRequest
            {
                Path = request.Path,
                LanguageCode = request.LanguageCode,
                Properties = [
                    "title",
                    "content",
                    "format(createdAt, \"dd MMM yyyy\") as createdAt",
                    "format(updatedAt, \"dd MMM yyyy\") as updatedAt",
                    "length(content)",
                    "innerLinks",
                ]
            });

        var elementIndex = all.Data
            .Index()
            .Where(x => x.Item.Path.SequenceEqual(request.Path))
            .Select(x => x.Index)
            .FirstOrDefault();

        if (elementIndex != 0)
        {
            // TODO - search only real items
            var previous = all.Data.ElementAt(elementIndex - 1);

            result.Previous = new NeighborCard
            {
                Path = previous.Path,
                Title = previous.Title,
            };
        }
        
        if (elementIndex < all.Data.Count - 2)
        {
            var next = all.Data.ElementAt(elementIndex + 1);

            result.Next = new NeighborCard
            {
                Path = next.Path,
                Title = next.Title,
            };
        }

        return result;
    }
    
    [HttpGet("tags")]
    public Tag[] GetTags([FromQuery] string languageCode, [FromQuery] string[] path)
    {
        var values = cmsBackend
            .CountPropertyValues(new CountPropertyValuesRequest
            {
                Property = "tags",
                FromPath = path,
                LanguageCode = languageCode,
            });

        return values
            .Select(x => new Tag { Key = x.Key })
            .ToArray();
    }

    public ManuItem Map(SectionItem x)
    {
        return new ManuItem
        {
            Key = x.FileName,
            Path = x.FullPath,
            Count = x.GetAllChildren().Count(y => y.HasContent),
            Title = x.Title,
            Icon = x.MdFile?.GetValueOrDefault("icon") as string,
        };
    }

    public class Tag
    {
        public required string Key { get; init; }
    }

    public class GetCardsRequest
    {
        public required string[] Path { get; init; }
        public required string LanguageCode { get; init; }
        public required PaginationData Pagination { get; init; }
        public required string[] ContentTypes { get; init; }
        public string? Tag { get; init; }
    }

    public class ManuItem
    {
        public required string Title { get; init; }
        public required string Key { get; init; }
        public required string[] Path { get; init; }
        public required string? Icon { get; init; }
        public required int Count { get; init; }
    }
    
    public class DocsMenuSection
    {
        public required string Title { get; init; }
        public required string[] Path { get; init; }
        public required DocsMenuItem[] Children { get; init; }
    }
    
    public class DocsMenuItem
    {
        public required string? Title { get; init; }
        public required string[] Path { get; init; }
    }

    public class CardItem
    {
        public required string FileName { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required string ContentType { get; init; }
        public required string[] Path { get; init; }
        public required int Length { get; init; }
        public required string?[] Tags { get; init; }
        public required string?[] Projects { get; init; }
    }

    public class GetCardRequest
    {
        public required string LanguageCode { get; init; }
        public required string[] Path { get; init; }
    }
    
    public class CardDetail
    {
        public required string Title { get; init; }
        public required string Content { get; init; }
        public required string CreatedAt { get; init; }
        public required string UpdatedAt { get; init; }
        public required long Length { get; init; }
        public required MarkdownInnerLink[] InnerLinks { get; init; }
        public NeighborCard? Previous { get; set; }
        public NeighborCard? Next { get; set; }
    }

    public class NeighborCard
    {
        public required string Title { get; init; }
        public required string[] Path { get; init; }
    }
}