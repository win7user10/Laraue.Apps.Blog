using Laraue.Apps.Blog.ApiHost;
using Laraue.Apps.Blog.ApiHost.docTypes;
using Laraue.CmsBackend;
using Laraue.CmsBackend.Extensions;
using Laraue.Core.Exceptions;
using Laraue.Interpreter.Markdown;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<ExceptionHandleMiddleware>();

var cmsBackend = new CmsBackendBuilder(
        new CmsBackendOptions { DefaultLanguageCode = "en" },
        new MarkdownParser(
            new MarkdownTranspiler(
                new WriteOptions { GenerateHeaderLinks = true },
                new MarkdownInnerLinksGenerator())),
        new MarkdownProcessor())
    .AddContentType<Project>()
    .AddContentType<Article>()
    .AddContentType<Documentation>()
    .AddContentType<RootSectionDefinition>()
    .AddContentType<SectionDefinition>()
    .AddContentFolder("blog")
    .Build();

builder.Services.AddHealthChecks();
builder.Services.AddSingleton(cmsBackend);
builder.Services.AddSingleton<ISitemapGenerator, SitemapGenerator>();

builder.Services.AddOptions<SiteOptions>();
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("SiteOptions"));

var app = builder.Build();

var origins = builder
    .Configuration
    .GetRequiredSection("Cors:Hosts")
    .Get<string[]>() ?? throw new InvalidOperationException();

app.UseCors(corsPolicyBuilder =>
    corsPolicyBuilder.WithOrigins(origins)
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.UseMiddleware<ExceptionHandleMiddleware>();
app.MapControllers();
app.MapHealthChecks("/_health");
app.Run();