---
title: When not to use a nullable foreign key — model the empty state as a default row
description: Part 9 of building a Telegram task tracker solo. Building the issues layer of the web app, and the database design decision at its centre — when a nullable foreign key is the wrong choice and a real default row is better, shown through issues, epics, and the backlog.
type: article
createdAt: 2026-06-25 09:00
updatedAt: 2026-06-25 09:00
projects: [boards]
tags: [database-design, dotnet, aspnet-core, postgres, vue, devlog]
previousLink: telegram-mini-app-authentication-dotnet
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 9.
> The [previous article](telegram-mini-app-authentication-dotnet) gave the web app a real, authenticated backend. With that foundation in place, this one builds the first thing the app actually does — and runs into a database-design decision worth getting right.

This article builds the first real domain feature of the web app: the issues layer, the part that shows the tasks a user has captured and lets them be managed on a board. But the part worth reading is not the CRUD endpoints or the Vue components — it is a small modelling decision in the middle of it that I got wrong first, and that anyone building a similar feature will face.

The decision is this: issues are grouped into epics, and some issues belong to no epic — they sit in the backlog. How do you model "belongs to no epic"? The obvious answer is a nullable foreign key: `EpicId` is either set, or `null` for backlog. That answer is wrong often enough to be worth a whole article, and the better one — model the empty state as a real default row, not as `null` — is the thing to take away from this part of the build. The feature itself is the setting; this decision is the point.

## What an issue is

An issue is the thing the whole product is about: a captured task. It is the same unit the bot saves when a message comes in, now surfaced in the web app for management. The model is small:

```csharp
public class Issue
{
    public long Id { get; set; }

    [MaxLength(4096)]
    public required string? Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public long StatusId { get; set; }
    public Status? Status { get; set; }

    public TelegramMessage? TelegramMessage { get; set; }
    public long? TelegramMessageId { get; set; }
    // ... numbering, custom attributes
}
```

The content, timestamps, and owning `UserId` are unsurprising. The relationship that matters most here is `StatusId`: an issue always has a status, and the status is what connects it to everything above it. A `Status` is a single column on the board:

```csharp
public class Status
{
    public long Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = string.Empty;

    public long EpicId { get; set; }
    public Epic? Epic { get; set; }

    public int SortOrder { get; set; }

    public IList<Issue>? Issues { get; set; }
}
```

And the status belongs to an epic, via `EpicId`. So the chain is `Issue → Status → Epic`: an issue does not reference its epic directly — it reaches it through its status. That indirection is itself a decision worth a moment, and it sets up the bigger one.

## The decision: nullable foreign key, or a default row?

Here is the modelling decision the intro promised, in full — because it is the part of this build most likely to save someone else time.

Issues are grouped into epics, larger units of work, and a board's columns (statuses) belong to an epic. Most issues live in some epic; the rest sit in the backlog, belonging to none. So the schema has to represent "this issue belongs to no epic," and there are two ways to do it.

**The obvious way: a nullable foreign key.** Give the issue an `EpicId` that is either set or `null`, and let `null` mean "backlog." This is the natural first choice — it is the one we started with — because it maps directly onto how you think about it: an issue either has an epic or it doesn't.

It caused more trouble than it saved, and the reason generalizes well beyond this app. A nullable foreign key that *carries a meaning* — where `null` does not mean "unknown" or "not yet set" but specifically "the backlog" — turns that meaning into a special case that every piece of code has to remember. Queries sprout `WHERE EpicId IS NULL OR …` branches. Application code keeps asking "is this null, and if so, treat it as backlog." And questions that should have one uniform answer — *which board does this issue belong to? which space? which set of statuses applies?* — stop having one, because for backlog issues the relationship that would answer them is, by design, absent. As the model grew, every new feature that touched issues had to re-handle the null case, and the cost compounded.

There is a second, sharper problem: the backlog becomes unconfigurable. Real epics have a name and a colour, and a user can change them. The backlog, being `null` rather than a row, has nowhere to store any of that — it cannot have a name or a colour because there is no record to put them on. The moment you want the backlog to behave like the other epics (rename it, colour it, give it its own statuses), the nullable model has no answer. Your only options are to bolt on a separate table that stores the same fields a row in `epics` already has — duplicating the epic concept — or to accept that the backlog is permanently second-class and unconfigurable. Both are worse than just making it a real epic.

**The better way: model the empty state as a real default row.** Instead of `null` meaning backlog, every user gets a real **default epic**, and backlog issues belong to it exactly like any other issue belongs to any other epic. That is what the `IsDefault` flag on the epic model encodes:

```csharp
public class Epic
{
    public long Id { get; set; }

    [MaxLength(128)]
    public required string Name { get; set; }

    public Guid UserId { get; set; }
    public long SpaceId { get; set; }

    public bool IsDefault { get; set; }
    public IList<Status>? Statuses { get; set; }
    // ... timestamps
}
```

With a default epic, an issue *always* resolves to an epic through its status — the backlog included. Its path to its statuses, its space, and its owner is always present and uniform — there is no null to check, no "if backlog then…" branch anywhere. The special case disappears, not because it was handled well, but because it stopped existing.

The principle underneath is worth stating on its own, because it comes up far beyond epics and backlogs: **a nullable column that carries a hidden meaning is usually a missing row in disguise.** Whenever `null` starts to *mean something specific* — the default category, the unassigned bucket, the "none" option — that meaning almost always wants to be a real row you can point a foreign key at, not an absence you reconstruct in code everywhere. The nullable version looks simpler at the schema level and quietly pushes complexity into every query and every feature that follows. The default-row version costs one row per user up front and removes a whole class of special-case logic forever after.

To be clear, this is not "never make a foreign key nullable." A nullable foreign key is exactly right when `null` means *genuinely unknown* or *legitimately optional* — the classic example is an employee whose `manager_id` is `null` because they have no manager, or an order with no assigned courier yet. There, `null` means "no information / no relationship," which is what `null` is *for*. The problem is the other case: when `null` is not "unknown" but a specific, named thing your application treats specially — "this is the backlog," "this is the default category." That is `null` smuggling in a value, and a value belongs in a row. The test is simple: ask what `null` *means* in your column. If the answer is "we don't know" or "there isn't one," nullable is fine. If the answer is a particular case your code keeps branching on by name, you have a missing row.

The default-row approach is not free either, and it is worth being honest about its cost. Because the backlog is now a real row, that row has to exist for every user — and something has to guarantee it gets created. If a user can ever end up without their default epic (a missed step at registration, a migration that skips existing users, a code path that creates a user but forgets the epic), the app breaks in places that simply assume the backlog is there. The nullable model never had this risk, because "no epic" needed no setup. So the trade is real: the default-row model removes a pile of special-case query logic, but adds a setup obligation you must not get wrong. Here that obligation is met at registration — the default epic is created in the same first-launch step that creates the user (from the previous article), so by the time any issue could exist, the backlog already does. The key is that the creation lives in exactly one place; the danger is letting it spread to several, where one can be forgotten.

### A second trade-off: redundancy versus the extra join

The `Issue → Status → Epic` chain hides a related decision. To find an issue's epic, the database has to join through the status: `issues → statuses → epics`. I could have avoided that join by also storing `EpicId` directly on the issue — then it would be a single `issues → epics` join, faster to read.

The catch is that storing both `StatusId` and `EpicId` on the issue duplicates information: the status already knows its epic, so the issue's `EpicId` would have to be kept consistent with its status's epic at all times, or the data would lie. That is a real maintenance burden and a real source of bugs — every status change would have to remember to update the epic too.

This is the everyday version of a choice developers make constantly: keep it simple and correct, or denormalise for performance. We went with the simpler, non-redundant model — reach the epic through the status — because at this scale the extra join costs nothing measurable, while the duplicated column would cost ongoing consistency work and invite bugs. The faster `issues → epics` join only becomes worth its upkeep when there is real, measured pressure to optimise. Starting with the simplest correct model and denormalising later, when something actually demands it, is almost always the right order — you can always add the redundant column when you need it, but you can't easily get back the time spent chasing consistency bugs you introduced before you needed to.

## Writing the backend: controller, host service, core service

With the model settled, the feature is built in three layers, and following a single request down through them is the clearest way to see what each does. Take "create an issue." It enters at the controller, passes through the host service, and ends at the core service.

At the top is a thin [`IssuesController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/IssuesController.cs). Like the `UserController` from the previous article, it does almost nothing itself: it is an `[Authorize]`-protected set of endpoints that read the authenticated user from `HttpContext.User`, bundle the request, and hand off to the host service. `GetBoard` returns the board, and create/update/delete map to the host service's corresponding methods. The controller's only job is to translate HTTP into a service call.

Below it is the **host service**, [`IssuesService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiServices/IssuesService.cs):

```csharp
public interface IIssuesService
{
    Task<ColumnIssues[]> GetBoard(
        GetBoardRequest request,
        CancellationToken cancellationToken);

    Task Delete(DeleteIssueRequest request, CancellationToken ct);
    Task<long> Create(CreateIssueRequest request, CancellationToken ct);
    Task Update(UpdateIssueRequest request, CancellationToken ct);

    Task<IssueDetailDto> GetIssue(
        GetIssueRequest request,
        CancellationToken cancellationToken);
}
```

This is where the work that *isn't* a raw data change happens. `GetBoard` shapes issues into columns for the frontend. The create/update/delete methods check that the caller is allowed to do what they are asking, open a transaction if several changes must be atomic, and only then call down to the core to actually mutate data.

At the bottom is the **core service**, [`ICoreIssuesService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.Services/CoreIssuesService.cs), whose entire surface is create, update, delete:

```csharp
public interface ICoreIssuesService
{
    Task<long> Create(
        CreateIssueRequest request,
        CancellationToken cancellationToken);

    Task Update(
        long issueId,
        Action<UpdateSettersBuilder<Issue>> setters,
        CancellationToken cancellationToken);

    Task Delete(
        long id,
        CancellationToken cancellationToken);
}
```

So a create request travels controller → host service (checks permission, opens a transaction if needed) → core service (writes the row). Each layer adds exactly one kind of thing, and the next section makes the division explicit.

### The principle: what each layer is allowed to do

The split between the two service layers is deliberate, and the rule for each is short.

The **core service** changes data and does nothing else. It performs create, update, and delete, and that is *all*: it does not validate, it does not check permissions, it does not open transactions (though it can require one to already be open), it does not decide policy. It is the thin, reusable layer that knows how to change data correctly. Both the bot and the web API call into it, and neither wants the core second-guessing the caller's intent.

The **host service** owns everything else: validation, permission checks, transactions, and read operations shaped for its surface. It is the layer that knows *who* is asking and *whether* they are allowed.

Stated plainly: the core knows *how* to change data; the host decides *whether* a given caller may, and *under what conditions*. Keeping validation and permissions out of the core is exactly what lets the same core logic serve both the bot and the web API without either inheriting rules meant for the other.

### Permissions: who can touch which issues

The permission rule for this feature is simple to state — **a user may read or change an issue or epic only if they created it** — but where it is enforced matters. It is implemented in a dedicated [`AccessService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.Services/AccessService.cs):

```csharp
public interface IAccessService
{
    Task<T> GetAvailableEpics<T>(
        Guid userId,
        Func<IQueryable<Epic>, Task<T>> map,
        CancellationToken cancellationToken);

    Task<T> GetAvailableIssues<T>(
        Guid userId,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken);

    Task<bool> CanMoveToStatus(
        Guid userId, long statusId, CancellationToken cancellationToken);

    Task<bool> CanModifyStatus(
        Guid userId, long statusId, CancellationToken cancellationToken);
}
```

The `AccessService` is itself a *core* service — it sits in the same layer as the other core services and is called *by* the host services. Its job is to hand back a query scoped to a single user. `GetAvailableEpics` and `GetAvailableIssues` do not return data directly — they take a `userId` and a `map` function, and hand that function a query *already filtered to what the user is allowed to see*. The host service supplies what it wants to do with that restricted query (count it, project it, fetch one row), and the access service guarantees it can only ever operate over the user's own records. Permission is enforced by construction: there is no way to get an `IQueryable<Issue>` that includes someone else's issues.

The host services lean on this for their permission checks — instead of fetching an issue and then asking "does this belong to the caller?", they start from `GetAvailableIssues` and can only ever see the caller's own. So permission is not a separate validation step that could be forgotten; it is the only query the host is given.

Keeping access scoping in a core service like this, rather than baking it into a global query filter or the controller, is a deliberate choice with a forward-looking reason. Not every caller is an authenticated user. Later there may be background jobs, scheduled tasks, or internal processes that need to operate on issues with no user in context at all — and those must *not* be forced through a per-user scope, because there is no user to scope to. Because access scoping is an explicit service the host *chooses* to call with a `userId`, a job that has no user simply does not call it and works over all the data it needs. The rule today is "creator only"; when organizations arrive later in the series, only this service's implementation changes — every caller that uses it keeps working unchanged.

### The other services follow the same shape

Issues are the worked example, but epics and statuses have their own host and core services built on exactly the same pattern: a core service that only mutates data, a host service that validates, checks permissions through the access service, and manages transactions. Once the shape is established for issues, the rest of the domain is more of the same — which is the point of having a shape. There is little value in walking through each; they differ in their fields, not their structure.

### Transactions: opened in the outer layer

One detail in the host layer is worth calling out, because it is a place that is easy to overcomplicate: a transaction is owned by the *outermost* caller — only the side that initiated the whole operation should decide when it is fully complete and ready to commit.

The reason is that operations compose. A host method might call another host method, which calls a service, which does several writes — and if each level naively opened and committed its own transaction, an inner step could commit (or roll back) half of an operation the outer caller was still in the middle of. Worse, you would have transactions nested inside transactions, and not every database supports that. The common-but-heavy fix is a counter at the application layer: track how many "begin transaction" calls are outstanding and only really commit when the outermost one finishes. It works, but it adds machinery and makes the code harder to follow — every transaction-aware method has to participate in the bookkeeping.

The approach here keeps it simpler: the outermost operation owns the transaction, and inner operations just run inside whatever transaction is already active. They do not commit; they leave that to the caller that started everything. But there is a real subtlety — sometimes an inner service genuinely *does* need a transaction, because it writes to several tables and those writes must be atomic regardless of who called it. It cannot assume the caller opened one. That is what an `EnsureTransaction` helper is for: it joins the transaction already in progress if there is one, and opens a fresh one only if there is not. So a service that needs atomicity gets it either way, without ever opening a second, nested transaction. The rule stays clean — the outer caller decides when the operation is done — while inner services that need their own safety net still get it.

## Rendering issues: from prototype HTML to Vue components

With the API in place, the frontend can show real data. The visual design is not new — it goes all the way back to the prototype from [early in the series](prototyping-ui-with-ai-before-code), the clean HTML mock-up built before any real code existed. That prototype was always meant to become real components; this is where it does.

A quick note on how the flat HTML becomes Vue components, since this is not an area where we have production Vue experience or any claim to best practices. The AI can do the splitting on its own — hand it the markup and it will happily propose a component tree. We mostly did it the manual way instead: looking at the generated markup and splitting along the repetition, or asking the AI to produce a *single* component from a specific chunk of passed-in code. If the same `<div class="card">` block repeats for each issue, that is a `Card` component; a repeated column wrapper is a `Column`. The structure of the HTML suggests the boundaries — wherever a chunk repeats or has a clear single responsibility, it becomes its own component.

The reason for doing it a piece at a time, rather than letting the AI generate the whole component tree in one shot, is control over the process. Generating everything at once produces a large pull request to review all at the end, where it is hard to tell what changed and why. Building one component at a time keeps each step small and reviewable, and keeps the development sequence in hand — which matters more, to us, than saving a few minutes by letting the model do it all at once. The actual result is in the [components folder](https://github.com/win7user10/laraue-boards/tree/master/app/components) if you want to see where it landed.

### Talking to the API: hand-written clients

Each area of the API gets a small frontend client composable, in the same style as the `userApi` from the previous article. Epics have [`epicsApi.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/composables/epicsApi.ts), issues their own, and so on — each a thin set of typed functions that call endpoints through the authenticated client.

We write these by hand. That is worth flagging because it is not the only option: with a properly configured OpenAPI/Swagger document on the backend, these clients can be *generated* from the API definition, so the frontend types and calls stay in lockstep with the backend automatically. We have written them manually here — it is straightforward at this size and avoids the setup of getting Swagger generation correct — but for a larger API, or a team, generating the client from Swagger is usually the better trade. It is one of those things that is fine to do by hand until the API is big enough that hand-maintenance becomes the bottleneck.

### Fitting inside the Telegram frame: applyInsets

One Mini-App-specific detail caught me out and is worth passing on. A Telegram Mini App does not get the whole screen — it renders inside Telegram's own frame, with Telegram's controls (the header, buttons, the swipe-down area) overlapping the edges of the web view. If you lay out the app as though it owns the full viewport, those controls cover your interface.

Telegram exposes *insets* — the safe-area offsets describing how much space its own UI takes around the edges — and the app applies them in [`app.vue`](https://github.com/win7user10/laraue-boards/blob/master/app/app.vue) via an `applyInsets` step, padding the layout so content sits inside the frame rather than under Telegram's controls.

The honest part is that insets alone do not fully solve it. Applied naively, they can over-correct — you end up with large blank gaps between your app and the Telegram frame in some places, while *still* getting elements clipped by Telegram's controls in others, because the insets do not perfectly describe every client's chrome. Getting it right is a balancing act: apply enough inset to avoid overlap, but not so much that the interface floats in a sea of padding. Sometimes the better move is to detect that the app is running inside Telegram at all and adjust the CSS specifically for the Mini App case — a tighter layout that assumes the Telegram frame — rather than trying to make one layout serve both a normal browser and the Mini App.

A practical recommendation: debug this part locally, against a real Telegram client, and budget patience for it. There is no single clean answer; it is fiddly, per-platform UI work, and it can take many attempts to land on a layout that looks right across the clients you care about. Iterating quickly on your own machine (the ngrok setup from the previous article makes this possible) beats discovering the overlap problems after deploying.

## Where this leaves us

The web app now does something real. It shows a user's issues, arranged on a board, pulled live from an authenticated backend — the first actual feature in a product that until now was pure plumbing. Issues can be created, moved between statuses, and deleted, each change checked so a user only ever touches their own data. The capture half of the product (the bot) and the management half (the web app) are now both real.

This article deliberately did not walk through the line-by-line CRUD implementation — the method bodies, the request DTOs, the exact LINQ. That code is all in the repositories ([backend](https://github.com/win7user10/Laraue.Apps.Boards), [frontend](https://github.com/win7user10/laraue-boards)) for anyone who wants to read it; reproducing it here would have buried the parts that actually carry a lesson under a lot of routine plumbing. The structure and the decisions are the transferable part; the CRUD is just CRUD.

The piece most worth carrying out of this article is not the feature, though — it is the modelling decision at its centre: when a column starts to *mean* something by being `null`, that meaning usually wants to be a real default row instead. The backlog as a default epic, not a nullable foreign key, is one instance of a choice that turns up constantly, and getting it right removes a whole category of special-case code before it can accumulate.

## What comes next

The next article moves up a level in the domain model, to epics — how issues group into larger units of work, and what the board looks like once it is organised into more than a single backlog. It is also where I will tell a story I have skirted around: the product mistake that is the real reason management lives in the web app and not the bot. Building this feature was the *result* of that lesson; the next article is where the lesson itself gets told.