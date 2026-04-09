---
title: CMS Backend
type: project
tags: [CMS,.NET,C#]
description: This project is a backend API designed to serve content for static sites built from Markdown files.
createdAt: 2025-11-01
updatedAt: 2025-12-08
---
## Key Features
|              |                                                                      |
|--------------|----------------------------------------------------------------------|
| Language     | C#                                                                   |
| Framework    | .NET 9                                                               |
| Project type | Library                                                              |
| Status       | Active Development                                                   |
| License      | MIT                                                                  |
| Nuget        | ![latest version](https://img.shields.io/nuget/v/Laraue.CmsBackend)  |
| Downloads    | ![latest version](https://img.shields.io/nuget/dt/Laraue.CmsBackend) |
| Github       | [Laraue.CmsBackend](https://github.com/win7user10/Laraue.CmsBackend) |

## How the project was born
We wanted to store all articles in Markdown format. Preferably separately from the frontend part. With the ability to search,
filter by attributes, and anything else that comes to mind.
The existing options are either storing Markdown in the frontend part, or full-fledged CMSs that store content in databases.
We wanted to have our own solution that combines all of this.

## What Is Classic CMS
CMS, or Content Management System, is a software that allows users to create, manage, and modify website
content without needing specialized technical knowledge. Popular examples include WordPress, Drupal, and Joomla.

### Why A CMS Isn't Always The Best Approach
Using a CMS can save users a great deal of effort.
- It eliminates the need to worry about system architecture.
- Users can often select from free templates, avoiding the need to write code from scratch.
- Many aspects are already considered: from SEO to admin panels, and more.

This significantly reduces the time it takes for an application to reach production. However, CMS solutions also have
disadvantages that make them unsuitable for all situations:
- **Architectural Constraints:** They impose a specific architecture, which isn't always ideal. Some projects require
extensive customization, which can be challenging or impossible to achieve within a CMS.
- **Technology Stack Compatibility:** The CMS's technology stack may not always align with the rest of a company’s
infrastructure, leading to additional support overhead.
- **Security Risks:** The popularity of CMS platforms makes them prime targets for attackers who exploit known
vulnerabilities to steal databases, deface websites, or use them for malicious purposes.

## CMS And Reactive Frameworks
### What Are Reactive Frameworks?
Reactive frameworks are software development tools that prioritize building user interfaces that respond quickly
to changes and updates, often in real-time. They achieve this by automatically updating the UI when underlying data
changes, resulting in a more fluid and interactive user experience. They offer advantages like:
- Faster and easier development.
- Simplified code maintenance.

However, a key problem with sites built using reactive frameworks is that the complete HTML page may not be rendered
until after the page begins loading. While this may not always be noticeable to the user, it can be problematic for
search engine crawlers. They may need to execute JavaScript, wait for XHR requests, and process a significant amount
of data before the final page content is available for indexing. Although modern crawlers are increasingly capable
of handling this, slow XHR requests can still lead to crawlers abandoning the page before the complete
content is indexed.

### Improving SEO With Reactive Frameworks
There are two main techniques to address this:
- **Server-Side Rendering (SSR):** The server renders the JavaScript and returns the fully rendered HTML page.
This improves SEO and can improve performance for users with slower devices, as they avoid the initial JavaScript
rendering phase. However, in many cases, the benefits are outweighed by the increased server load. Since most devices
are capable of handling JavaScript rendering efficiently, server-side rendering often just increases load times.
While it might ease the life of crawlers, it can degrade the experience for human users.
- **Static Site Generators (SSG):** This approach generates the Vue pages' content and delivers a static site with
JavaScript support. It's an interesting concept, but it has limitations regarding customization, component usage,
and is best suited for truly static sites. Displaying dynamic content can be challenging or impossible.

## Technical Requirements For This Blog
After some research into potential solutions for the blog, several requirements became clear:
- The blog’s frontend should be flexible enough to implement any idea without being constrained by the architecture
of a specific library.
- Page content should load as quickly as possible to maximize the chances of correct indexing by search engine crawlers.
- The approach of using Markdown files was particularly attractive, as these files can be stored in Git for version
control, eliminating the need for a database.

## Library Vision
Given the need for complete frontend flexibility, loading specific articles wherever desired is a key requirement.
Therefore, the focus will be on the backend.

The main requirements for the backend library are:
- Ability to structure and manage Markdown files.
- Support for front matter to store attributes.
- Defined content types, such as `Article` which must include a required `string Tags[]` property. This prevents authors
from accidentally omitting necessary information, ensuring the backend functions correctly.
- An API to retrieve files, filtering, sorting, or returning specific content, such as a list of unique tags from all
articles sorted alphabetically.

## How To Use The Library
To use the library, content types must be defined first.
```csharp
public class Article : BaseContentType
{
    public required string[] Projects { get; init; }
    public required string Description { get; init; }
}
```
Then content should be written – for example, `article1.md`
```markdown
---
title: About my project
projects: [Project1, Project2]
description: My short description
---
The markdown content
```
It’s recommended to organize the content to match the project structure on the frontend.
- blog
    - articles
        - article1.md
        - article2.md

Build the host:
```csharp
var cmsBackend = new CmsBackendBuilder(new MarkdownParser(new MarkdownToHtmlTransformer(), new ArticleInnerLinksGenerator()), new MarkdownProcessor())
    .AddContentType<Article>()
    .AddContentFolder("blog")
    .Build();
```

And add controller endpoints:
```csharp
[ApiController]
[Route("api/blog")]
public class BlogController(ICmsBackend cmsBackend) : ControllerBase
{
    [HttpPost("single")]
    public Dictionary<string, object> Get([FromBody] GetEntityRequest request)
    {
        return cmsBackend.GetEntity(request);
    }
    
    [HttpPost("list")]
    public IShortPaginatedResult<Dictionary<string, object>> Get([FromBody] GetEntitiesRequest request)
    {
        return cmsBackend.GetEntities(request);
    }
}
```

Now the frontend can call the backend and request only the data it needs. Of course, endpoints can be defined statically,
with each endpoint corresponding to a specific DTO and action. Alternatively, the HTML could be generated on the backend
using MVC controllers. The core principle is that the CMS Backend provides an API to retrieve Markdown files and their
properties. The other is user decision.

## Challenges
The specific problems and solutions will be described in separate articles:
- How to write Markdown renderer

## Timeline
- **Sep 2025** CMS investigations to choose a solution for the blog.
- **Oct 2025** First version of CMS Backend.

## Further Ideas
- The system can not only be the backend for frontend API, but also has options to make posting in different systems.
The package `Laraue.CmsBackend.Telegram` can contain tools to run the bot which will post new articles to channels by specified
criteria. Posts can have attributes to separate posts to different distribution channels.