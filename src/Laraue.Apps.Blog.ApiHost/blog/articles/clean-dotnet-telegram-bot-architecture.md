---
title: A clean .NET Telegram bot architecture — controllers, not a giant switch statement
description: Part 5 of building a Telegram task tracker solo. A clean .NET Telegram bot with ASP.NET-style controllers and middleware instead of a giant switch, a layered solution, and using EF Core and linq2db together on one set of models for the queries EF Core handles badly.
type: article
createdAt: 2026-06-21
updatedAt: 2026-06-21
projects: [boards]
tags: [dotnet, telegram, postgres, ef-core, linq2db, clean-architecture, devlog]
previousLink: choosing-stack-for-solo-project
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 5.
> The first four articles were groundwork: why, what, the user path, the stack. This is where it becomes code.

The goal of this first iteration is small and concrete: a backend that can receive a message from a Telegram user and store it. No web API, no frontend, no boards yet — just the smallest slice of the system that does something real. Everything in this article exists to support that one capability, and it is all set up before a single thing is deployed.

## One host, not a platform

The first version of the backend is a single runnable project: a Telegram host. It connects to Telegram, receives updates, and writes to the database. That is the entire surface area at this stage.

It is tempting, when you can already see the whole product in your head, to scaffold everything up front — the web API, the background workers, the services for features you know are coming. We did not. The host is the only entry point the first user path needs, so it is the only entry point that exists. Projects that are not yet required do not get created, the same way tables that are not yet required do not get added.

## The solution structure

Even with a single entry point, the code is layered from the start, because the layering is cheap to set up early and expensive to introduce later. The solution is organised the conventional way: a `src` folder holds all the projects, and a `tests` folder holds the tests. Each project is a separate `.csproj` under `src`. If you would rather see the real thing before reading the explanation, the [whole solution is in the backend repository](https://github.com/win7user10/Laraue.Apps.Boards/tree/main/src) — the rest of this section is a walk through what is in it and why.

A note on how this gets created, because it is a place where our solo approach differs from big tech. We create the projects manually — adding each `.csproj` through the code editor's context menu, by hand. In larger organisations this is usually automated: there is a template that scaffolds a new project or service with the correct structure, naming, and defaults, generated from the terminal or the IDE so that nobody introduces mistakes or drift. That automation earns its keep when new solutions for new microservices are being spun up almost daily — the template is what keeps hundreds of services consistent. We do not have that volume. A handful of projects, created once, does not justify building and maintaining a scaffolding template, so we do it by hand. It is the same judgement that runs through the whole series: adopt the heavyweight practice when the scale actually calls for it, not before.

### The projects, and the naming convention

The backend is organised into separate projects, each with one responsibility. The naming follows the convention we use across all our projects — the same pattern big tech tends to use, where every project is prefixed with the product namespace:

- **`Laraue.Apps.Boards.DataAccess`** — the EF Core models, enums, and the database context. No business logic lives here; it is purely the shape of the data and how it is persisted.
- **`Laraue.Apps.Boards.Services`** — the core business logic, shared across every host. A core service does the full, surface-agnostic work of an operation. Creating an issue, for example, is a core function: it creates the issue, writes the audit log entry, updates the board's last-touched date, and whatever else that operation entails to leave the data consistent. Core services do not own transactions, but a service can require that one already exists.
- **`Laraue.Apps.Boards.TelegramServices`** — host-specific services for the Telegram surface. These reference the core services and wrap them with the concerns specific to this entry point. Continuing the example: the Telegram-specific "create issue" service checks whether the user is permitted to create an issue, and only if they are does it call the core create-issue service to do the actual work.
- **`Laraue.Apps.Boards.TelegramHost`** — the runnable project itself, wiring everything together and talking to Telegram.

The consistent `Laraue.Apps.Boards.*` prefix is not decoration. It makes the dependency direction obvious at a glance, keeps namespaces unambiguous when projects are referenced across the solution, and means anyone opening the repository can read the architecture from the project list alone. It is the kind of small, boring convention that pays off every time someone navigates the code.

### One host per function

The host project's name follows a related rule: a host is named for its primary function. `TelegramHost` runs the Telegram bot; later in the series a `WebApiHost` will serve the HTTP API; in other projects you might have a `RabbitWorkerHost` consuming a queue, and so on. The name tells you immediately what the process does.

This is not just a naming habit — it reflects a deliberate decision to keep different functions in different hosts. The bot, the web API, a background worker: each is a separate runnable process rather than one host doing everything. The payoff is operational flexibility. Each host can be given its own resource limits (the bot and a heavy background worker have very different memory and CPU profiles), its own network rules (the web API is exposed publicly, a worker may need no inbound access at all), and its own scaling and restart behaviour. Bundling everything into a single process throws all of that away and forces one set of limits onto workloads that do not share the same needs. Splitting by function keeps each one independently tunable.

The Telegram host is a concrete example of why the network rules matter. Because it uses long polling — it reaches out to Telegram to fetch updates rather than receiving webhooks — it needs no inbound access at all. Nothing from the outside world ever has to connect to it. So it is not exposed publicly: no open port, no public route, nothing for the internet to reach. That is only possible because it is its own host. If the bot shared a process with a public web API, it would inherit that process's exposure whether it needed it or not. Keeping it separate lets it stay completely closed off, which is exactly what a process that only talks outward should be.

### Core services and host-specific services

The rule tying these together: a host only ever references its own host-specific services, never the core services directly. The Telegram host talks to `TelegramServices`, which in turn call into the core `Services`. This draws a clean line between two kinds of logic. Core services hold what is true regardless of where a request came from — an issue is created the same way, with the same audit log and the same side effects, whether the request arrived from a bot, a web API, or anywhere else. Host-specific services hold what is true only for that surface — permission checks, surface-specific validation, how the request is shaped.

That division is what makes adding a second entry point cheap later. When the web API arrives, it gets its own host-specific services that apply its own permission and validation rules, and then call the very same core services the bot already uses. The valuable, consistency-critical logic — creating an issue correctly — is written once and reused. Only the surface-specific wrapper is written per host. Without this separation, the second entry point would either duplicate the core logic or tangle permission checks into code that should not know about them.

This split is not free, though, and it is not always worth making. It earns its keep only when logic genuinely will be shared across more than one host. If we are confident a piece of functionality will only ever live behind a single host — something inherently specific to the Telegram bot, say, that no web API or worker would ever call — then splitting it into a core service and a host-specific wrapper is just ceremony. In that case we keep it simple: the logic lives in one place, in the host-specific layer, with no core counterpart. The core-versus-host-specific division is a tool for sharing, so we reach for it where sharing is real and skip it where it is not. Guessing wrong in the cautious direction — splitting something that turns out to be single-host forever — costs a little extra indirection; guessing wrong the other way costs a refactor later. Neither is a disaster, so we make the call case by case rather than applying the split everywhere on principle.

## The first schema

The schema follows directly from the user path defined earlier: a user writes a message to the bot, and it becomes something that can later be organised on a board. So the first tables are the minimum that supports that — a user, the message they send, and the structures needed to place that message somewhere: a category and a status.

The central table is `Message`. At this stage that name is literally accurate — the thing being stored *is* a Telegram message:

```csharp
public class Message
{
    public long Id { get; set; }

    [MaxLength(4096)]
    public required string? Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // The user the message belongs to.
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // The message's current status.
    public long StatusId { get; set; }
    public Status Status { get; set; }

    // Category it belongs to. Null for backlog.
    public long? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Linked Telegram message. Null means it was not created through Telegram.
    public TelegramMessage? TelegramMessage { get; set; }
    public long? TelegramMessageId { get; set; }
}
```

A few things in this model are worth pointing out, because they reflect decisions that run through the whole schema.

The link to Telegram is optional. `TelegramMessageId` is nullable, and the comment says why: a null means the message was not created through Telegram. Even in the first iteration, where every message *does* come from the bot, the model already allows for messages created some other way. That is not over-engineering — it is one nullable column that keeps a door open at no cost.

`User` has a `Guid` id and implements `ITelegramUser<Guid>`, an interface from our [Laraue.Telegram.NET](https://github.com/win7user10/Laraue.Telegram.NET) library, which carries the Telegram identity fields — Telegram id, username, language code, first and last name. The library knows how to populate a user from a Telegram update; our model just has to implement the interface.

`Category` and `Status` are the placement structures. A status is a column a message sits in; a category groups messages. Both carry a `TouchedAt` timestamp — the "last touched" date that the create/update operations keep current, so categories can be sorted by recent activity. This is the same `TouchedAt` the core service example earlier was updating when an issue changes.

### Always limit the length of string columns

There is one rule visible in every model above that is worth calling out on its own: every string column has an explicit maximum length. `Content` is capped at 4096, names at 128, and `Color` — a hex code like `#1D9E75` — at exactly 7.

This is a default we never skip. An unbounded string column is `text`/`varchar(max)` under the hood, and that is a problem on several fronts. It is an open invitation for unbounded data — without a limit, nothing stops a single row from holding megabytes, whether through a bug, abuse, or a user pasting something enormous. It removes a validation boundary that belongs in the schema, where the database enforces it, rather than relying on every code path to check. And it can cost performance: bounded columns let the database make better decisions about storage and indexing than open-ended ones.

Choosing the limit also forces a small, useful moment of thought. Capping `Color` at 7 means deciding, deliberately, that this column holds a `#RRGGBB` hex code and nothing else. Capping `Content` at 4096 means deciding what the longest reasonable message is — and 4096 is not arbitrary; it is Telegram's own message length limit, so the column matches the real constraint on the data it stores. Every length is a small decision about what the data actually is, made once, at the right place.

### What the schema deliberately leaves out

This is the user-path-first principle in practice. We did not sit down to design "the schema for a task tracker." We designed the schema for the actions the first version supports, and stopped. Everything the product will eventually need — epics, spaces, organizations, permissions, custom attributes — is absent, because none of it is in the user path yet. It gets added later, each piece when its feature arrives, and the schema grows with the product rather than ahead of it.

### The mistake: renaming the core entity three times

There is one decision from this period that cost real time, and it is worth being honest about: we could not settle on what to call the central entity.

The `Message` class shown above is what it looked like *at this stage* — and that name made sense then, because the thing being stored literally was a Telegram message. But it did not keep that name. As the board concept took shape, **message** felt wrong; these were cards on a board, so it became a **card**. Later, as the product matured toward a real tracker with epics and hierarchy, **card** felt too lightweight, and it became an **issue** — which is its name today, in the code, the UI, and throughout this series. The model above is a snapshot of the first stage; in the current schema that same class is `Issue`.

Each rename was not just a find-and-replace. The name was woven through the models, the services, and the data already stored under the old name. Every rename meant generating a new migration to rename the underlying table and applying it to a database with real rows in it, without losing anything. Three renames, three rounds of that.

#### What we took from it

We do not have a clean rule for how to have avoided it. The right name genuinely was not knowable at the start — **message** was correct for what the thing was in the first iteration, and only became wrong as the product changed underneath it. More upfront domain modelling might have caught it, but that has its own cost, and over-investing in the name of an entity whose role is still forming is its own kind of waste. The most we took from it: a core entity's name is expensive to change once there is data under it, so it is worth a little more thought than an average naming decision — but not enough to stall the first iteration over. And it is worth remembering this happens to mature products too: while we were building Boards, Atlassian renamed two of Jira's most fundamental concepts, 'projects' to ['spaces'](https://community.atlassian.com/forums/Jira-articles/Jira-Spaces-have-landed/ba-p/3117620) and 'issues' to 'work', for exactly the same reason — the original names had quietly become misleading as understanding of the product matured. A solo project renaming `message` to `issue` is in good company.

#### The project got renamed too

The entity was not the only thing that got renamed. The project itself started life as `Laraue.Apps.StructuredMessages`, because at the very beginning we did not know what the product would be called either. Once it became clear it was Laraue Boards, that name had to be carried through everything — the repository, the solution and all its projects, the namespaces, the deploy files. We did that as its own dedicated pull request, with no other changes in it, the way we treat any large mechanical rename: a pure rename is only easy to review and to revert if it is not tangled up with real feature work. It is the entity-naming lesson again, one level up — the right name for the whole thing was not knowable at the start, and changing it later was costly but manageable as long as it was done cleanly and in isolation.

## How a message actually gets stored

The schema is where a message ends up. The more interesting part is how it gets there, because the path it takes is the architecture from earlier made concrete.

Two kinds of Telegram update arrive at the bot: commands, and everything else. They are handled by two different mechanisms.

Commands are routed to controllers, ASP.NET-style, by the [Laraue.Telegram.NET](https://github.com/win7user10/Laraue.Telegram.NET) library. The `/start` command, for instance, is just a method with a route attribute:

```csharp
public class CommandsController(ITelegramCommandsService commandsService)
    : TelegramController
{
    [TelegramMessageRoute("/start")]
    public Task HandleStart(
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return commandsService.HandleStart(
            ReplyData.FromMessageRequest(requestContext),
            cancellationToken);
    }
}
```

This is the pattern that makes the bot pleasant to work on, and it is worth dwelling on because it is the exception rather than the rule in .NET bots. Most Telegram bot examples in C# funnel every update into one giant handler — the "god method": a single method with a deep nested switch on command text, `if`/`else` chains, and all the command logic piled together. It works for three commands and becomes unmaintainable by ten. Routing each command to its own small controller method, the way you would route HTTP endpoints, avoids that entirely. Each command is a self-contained, declarative handler. If anyone has written an ASP.NET controller, they can read this immediately and add a new command without touching any existing one.

But a plain message — someone typing a task, not a command — matches no route. That is the common case for this product, and it is handled by a middleware that runs as a fallback. This is not ASP.NET middleware, even though it looks and behaves like it. It is part of [Laraue.Telegram.NET](https://github.com/win7user10/Laraue.Telegram.NET)'s own request pipeline, which we deliberately modelled on ASP.NET's so that the familiar concepts — routing, controllers, middleware — carry over to Telegram update handling. The point of the library is exactly this: make handling bot updates feel like handling HTTP requests, so a .NET developer already knows the shape of it. The middleware here is the library's, not the framework's, but it works the way you would expect from the name.

It only acts when no route was executed, so commands always win; anything left over is treated as a message to capture:

```csharp
if (context.GetExecutedRoute() is null && AllowedUpdates.Contains(context.Update.Type))
{
    var message = context.Update.Message ?? context.Update.EditedMessage;

    SaveMessageTelegramRequest? request = message.Type switch
    {
        MessageType.Text => GetMessageRequest(message),
        // photo, video, animation handled here too, later in the series
        _ => null
    };

    if (request is not null)
        await telegramMessageService.HandleSaveMessage(request, ct);
}
```

For a text message, the middleware builds a `SaveTextMessageTelegramRequest` — a plain object carrying the text, the Telegram message id, the user, the timestamp, the sender. The raw Telegram update is translated into the application's own request type right here at the boundary, so nothing deeper in the system has to know about Telegram's update format.

That request then flows down through the layers exactly as the structure section described:

1. **The middleware (in the host)** translates the Telegram update into a `SaveTextMessageTelegramRequest` and hands it to the Telegram message service.
2. **The Telegram message service (in `TelegramServices`)** is the host-specific layer. It deals with what is true for the Telegram surface — resolving the request into a save operation — and then calls the core service to do the actual work.
3. **The core issues service (in `Services`)** performs the surface-agnostic operation: create the issue, or update it if this message has been seen before. This is the same core method that the web API will call later. The bot is simply its first caller.

This is the whole point of the layering, shown end to end on the very first feature. A message comes in from Telegram, gets translated to a request at the edge, passes through the Telegram-specific service, and lands in a core service that knows nothing about Telegram and will one day be called from somewhere else entirely. The capture feature is small, but it is built on the seam that the rest of the product will be assembled along.

## Building the host

All of this is wired together in a `Program.cs` that stays deliberately short. The whole host startup is about two dozen lines:

```csharp
var builder = WebApplication.CreateBuilder(args);

const string dbConnectionStringName = "Postgre";

builder
    .AddTelegramOptions("Telegram")
    .AddApplicationServices()
    .AddDatabaseServices(dbConnectionStringName);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.Services.UseLinq2Db();

using (var scope = app.Services.CreateScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();
    app.MapTelegramRequests();
}

app.MapHealthChecks("/_health");

app.Run();
```

The registration is grouped into a few extension methods — `AddTelegramOptions`, `AddApplicationServices`, `AddDatabaseServices` — so the entry point reads as a summary of what the host is rather than a wall of `services.AddX()` calls. Each layer registers its own dependencies behind one of these methods.

A few things happen at startup, in order: health checks are registered; the app is built; linq2db is initialised over the existing setup; the database is migrated to the latest schema with `MigrateAsync()` so the running host always matches the current migrations; the Telegram routes and middleware are mapped; and the health endpoint is exposed. Then it runs.

That `await db.Database.MigrateAsync()` call inside startup is another place where our solo approach diverges from corporate practice. In larger setups, applying migrations is usually a separate, explicit step in the deployment pipeline — a dedicated job that runs the migrations against the database before the new version of the application starts, often with review gates, rollback plans, and careful sequencing for zero-downtime releases. There are good reasons for that at scale: you do not want every instance of a horizontally-scaled service racing to migrate the same database, and you want migration failures to stop a deploy cleanly.

We do not have those constraints. There is one instance of the host, one database, and one person deploying. So we migrate on startup: the host brings the database up to its own schema when it boots, and there is no separate migration step to build, run, or remember. Deploy the host, and it makes the database match. It is the simpler path, chosen deliberately to avoid overcomplicating a deployment that does not need the machinery a larger system would. If Boards ever grew to multiple instances, this is one of the first things that would move out into a dedicated deploy step — but until then, the simple version is correct.

### Creating migrations, and keeping them tidy

That covers applying migrations. Creating them is a separate, manual step in development. From the `src` folder, a new migration is generated with the EF Core CLI, pointing at the DataAccess project (where the context and models live) as the migrations project and the host as the startup project:

```bash
dotnet ef migrations add MigrationName \
  -p Laraue.Apps.Boards.DataAccess \
  -s Laraue.Apps.Boards.TelegramHost \
  -v
```

That produces a migration which then gets applied automatically on the next host start, as described above.

There is a habit attached to this that is worth passing on: try to keep each pull request down to a single migration. While working on one PR, it is normal to generate several migrations as the schema is figured out — add a column, change a type, add an index, realise the first cut was wrong. Before the PR is done, those get collapsed into one. Sometimes that means merging them by hand, moving the generated code from one migration file into another where it can be combined. Other times it is cleaner to delete all the new migrations, roll the `DatabaseContextSnapshot` back to where it started, and generate a single fresh migration from the final schema.

The reason is long-term navigability. A migration is permanent history, and a project that lives for years accumulates a great many of them. An endless list of tiny migrations is hard to read through when you are trying to understand how a table evolved, and because migrations are applied in sequence, initialising a fresh database on a new developer machine replays every one of them — which, in rare cases with a long history, can become noticeably slow. This is not a solo-project quirk either: in big tech you will see deliberate effort spent merging old migrations, specifically to shrink the migration code in the repository and improve that first-init experience. The rule we aim for is simple: ideally, no more than one migration per pull request.

### EF Core and linq2db on the same models

One line in that startup is worth its own explanation: `app.Services.UseLinq2Db()`. It reflects a data-access approach we use across all our projects — EF Core as the primary ORM, with linq2db available through an adapter for the queries EF Core handles badly, both running on a single set of models.

The idea is this. EF Core is the default for everything: the models, the migrations, the everyday queries. But EF Core cannot express every query well — bulk inserts and updates, certain complex joins, and database-specific SQL constructs are awkward or impossible through it. For those, linq2db is used, and the key detail is that it runs on the *same* EF Core models. The bridge that makes this possible is the official [`LinqToDB.EntityFrameworkCore`](https://github.com/linq2db/linq2db/tree/master/Source/LinqToDB.EntityFrameworkCore) adapter, which reads all the metadata it needs from the EF Core annotations and configuration, so linq2db uses the schema EF already defines. There is no second set of mappings, no duplicate model. The same entity class is queried through EF Core most of the time and through linq2db when EF cannot handle the case, with the adapter keeping them in sync because there is only ever one source of truth for the schema.

The `UseLinq2Db()` call in `Program.cs` comes from our own thin wrapper around that official adapter. We keep small integration layers like this in our shared [Laraue.Core](https://github.com/win7user10/Laraue.Core) library — adapters that take a third-party package and wire it into our applications the way we want it. The [linq2db wrapper](https://github.com/win7user10/Laraue.Core/blob/master/src/Laraue.Core.DataAccess.Linq2DB/Extensions/ServiceCollectionExtensions.cs) is small: it runs the official adapter's initializer (`LinqToDBForEFTools.Initialize()`) and routes linq2db's internal tracing into the standard Microsoft `ILogger` pipeline, so linq2db's query logs show up alongside everything else. Then it exposes the two clean extension methods — `AddLinq2Db()` to register it and `UseLinq2Db()` to initialise it — that the host calls. The official adapter does the real work; the wrapper just makes it a one-line, consistent setup across every Laraue project.

The natural question is why not use linq2db for everything, since it is a powerful query tool. Two reasons. First, the relationship runs both ways: there are cases EF Core handles that linq2db does not, just as there are cases linq2db handles that EF does not — keeping both means always having the right tool. Second, EF Core brings things linq2db does not aim to: the change-tracking, active-record-style pattern that is genuinely convenient for a lot of ordinary create-and-update work, and a migrations experience that is, frankly, pleasant to use. We did not want to give those up.

So the rule is EF Core by default, linq2db where EF falls short, both over one set of models. It is the same instinct as the rest of the stack: use the comfortable, well-supported default for the common case, and reach for the specialised tool only at the specific points where the default genuinely cannot do the job — but here the two coexist on the same schema rather than one replacing the other.

## Healthchecks from day one

You will have noticed two health-related lines in that `Program.cs` — `AddHealthChecks()` and `MapHealthChecks("/_health")`. They go into every host from the very first commit, before there is anything to monitor and before it is ever deployed.

A request to `/_health` returns whether the host is alive and able to serve. It costs nothing to add now, and adding it from day one means every piece of infrastructure that comes later — the container orchestration, the reverse proxy, an uptime monitor — has something to call without anyone going back to retrofit it. When a container runtime needs to know whether to restart a process, or a load balancer needs to know whether to route to it, the endpoint is already there.

It is the cheapest possible insurance: in place before it is needed, rather than scrambled together the first time something falls over in production.

## Where this leaves us

At the end of this iteration there is a layered .NET backend with a single Telegram host. It receives a Telegram message, routes commands through controllers and plain messages through the fallback middleware, translates them into requests at the edge, and passes them down through a host-specific service into a surface-agnostic core service that writes to a minimal PostgreSQL schema. The host migrates its own database on startup and exposes a health endpoint. EF Core handles the data access, with linq2db ready for the queries EF cannot.

Nothing is deployed and nothing is running in front of users yet. The code builds and works locally, but it has never left the developer's machine. Everything needed to capture a task exists; nothing needed to put it on a server does.

## What comes next

The next article takes this host and gets it onto a real server — the VPS it will run on, a self-hosted PostgreSQL alongside it, the build-and-ship pipeline that delivers it, a Dockerfile and Docker Compose to run it, and long polling to connect it to Telegram without exposing it to the internet. It also runs into the first real product mistake: trying to make the bot do too much.