---
title: Choosing a stack for a solo project — .NET, PostgreSQL, Nuxt, and why boring won
description: Part 4 of building a Telegram task tracker solo. The real reasons behind .NET 10, PostgreSQL 18, Nuxt 4 and Vue 3 — and a MongoDB-to-Postgres rewrite from a past project that taught us to default to boring, stable technology.
type: article
createdAt: 2026-06-20 22:35
updatedAt: 2026-06-20 22:35
projects: [boards]
tags: [dotnet, nuxt, vue, postgres, mongodb, database, devlog, architecture]
previousLink: telegram-saved-messages-to-task-tracker
nextLink: clean-dotnet-telegram-bot-architecture
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 4.
> The [previous article](telegram-saved-messages-to-task-tracker) defined the user path. With the prototype and the path in hand, we finally knew enough to choose the tools.

Stack decisions came after the prototype and the user path, not before — and that order matters. Choosing a database or a framework before you know what the product does is guessing. By this point we knew what the screens looked like and what the user actually did, so each choice could be made against real requirements rather than taste.

The short version: .NET 10 and C# on the backend, PostgreSQL 18 for data, Nuxt 4 and Vue 3 with TypeScript on the frontend. None of these are exciting choices, and that is the point. We covered the general reasoning — [prefer stable technologies, keep dependencies minimal](how-we-build-engineering-principles) — as a principle; here is how it applied to each part of the stack, and the mistake that taught us the principle in the first place.

## Backend: .NET 10 and C#

.NET is our primary professional stack, and for this project that settled it. But "use what you know" is not an absolute rule — it depends entirely on what your goal is.

If your goal is to learn a technology, the best way to do it is to build a real project with it. Nothing teaches a stack like shipping something non-trivial on it. But you have to go in knowing the path will be much longer than you expect, because learning and building happen at the same time: you reach for a pattern, learn a better one, and want to refactor — and that repeats, again and again, as your understanding deepens. The web crawler from later in this article was partly that kind of project. One of its goals was to push our C# from junior to a solid middle level, coming from a middle-plus background in PHP. It worked — but the project was rewritten three or four times to get there. Those rewrites were the cost of learning, and on a learning project that cost is the point.

This project was different. The goal was to ship a real product solo, quickly and without unforced mistakes — not to learn a stack. When that is the goal, you reach for what you already know deeply, because the rewrites that teach you a new technology are exactly the delays you cannot afford. Every hour spent fighting an unfamiliar ecosystem is an hour not spent on the product, and the novelty should be in the product, not the tooling.

So the rule is conditional: building on a new stack is one of the best ways to learn it, and a bad way to ship fast. Know which goal you are optimising for before you choose. For Laraue Boards the goal was shipping, so the choice was the language we already knew best.

One specific thing .NET gave us: a bot library we had already written. The Telegram integration is built on [Laraue.Telegram.NET](https://github.com/win7user10/Laraue.Telegram.NET), our own library that lets you handle Telegram updates with ASP.NET-style controllers. We know exactly how it works because we wrote it, and we can extend it without waiting on anyone. More on that when the bot enters the series.

## Database: PostgreSQL 18

PostgreSQL is the database for the same reason as .NET — we know it performs well enough for everything this project will face, and its extension ecosystem covers nearly any scenario we might hit later: full-text search, partitioning, time-series data, whatever comes. It is not the only valid choice. It is the one we trust, and "the one we trust" is worth more than "the one with the best benchmark" when you are the entire team.

That trust is not theoretical. In big-tech production we watched SQL Server get replaced with PostgreSQL on a high-load system, and it ran stably under that load. Once you have seen Postgres hold up in that environment, there is very little a solo task tracker can throw at it that raises any doubt. And it is free, which when you are paying for your own infrastructure is not a small thing. We knew, before writing a line, that Postgres would be more than enough for Boards.

It is also relational, and that turns out to be the whole story — because the most useful thing we can say about this decision is the time we got it wrong on a different project.

### The MongoDB mistake — and the rewrite to PostgreSQL

That same web crawler taught us a second lesson, about the database. We wanted to use MongoDB — not because the project needed it, but because it was the interesting choice, and we went looking for reasons to justify it. We told ourselves a document store fit the shape of crawled data, that the flexibility would help, that the schema-less model suited an evolving crawler.

The honest truth was that the project needed a relational database for most of what it did. The data had relationships, and we were reverse-engineering a justification for a tool we had already decided we wanted.

It held up for a while, and then it did not. As the project grew and needed more relations between entities, queries that would have been trivial joins in a relational database became awkward, slow, and hard to express in MongoDB. We kept trying to bolt relational patterns onto a document store — effectively rebuilding a relational database inside Mongo, badly. Eventually we conceded and rewrote the whole thing on PostgreSQL.

That rewrite is the lesson. Some wrong choices can be refactored; this one could only be thrown away and redone. The cost was not "Mongo was a bit awkward" — it was migrating an entire working project to a different database because the original choice was made for excitement rather than fit.

### The rule it produced

Now the path is always the same: default to a boring relational database, and only look toward modern or highly specialised technology when the boring option genuinely cannot do the job. Not when the specialised option seems more interesting. Not when you can construct a plausible-sounding reason. Only when the standard tool actually fails the requirement in front of you.

For Laraue Boards, the standard tool does not fail — and we knew that early, because of how we approach a database at the start of a project.

### Imagining the hard cases before committing to relational

When we start thinking about data, we think high-level first: what tables the domain probably needs and how they relate to each other. This is not about producing a final schema — the schema will change. It is about picturing the most difficult cases we are likely to meet, so we can check whether the database we are about to choose can handle them at all. If the hard cases fit comfortably in a relational model, the easy ones certainly will.

For Boards, a few hard cases were visible from the start:

**Full-text search.** We knew we would need FTS to search issues. PostgreSQL has it built in — no extra service, no separate search engine to run and sync.

**The core shape.** Tables for users, for messages received from Telegram, and for boards related to those messages. All clearly relational, with clear relationships between them.

**Custom attributes — the hardest case.** We knew we wanted user-defined attributes on issues, and we wanted to try implementing them as a table per attribute type rather than a JSON blob. The attribute definitions live in one table — name, colour, type — with extra per-type detail in tables like `text_attributes` or `date_attributes` depending on the type chosen. The actual values go in typed tables: `messages_number_attributes` with a `message_id` and a `long` value, `messages_string_attributes` with a `message_id` and a `varchar` value, and so on. This shape works beautifully in a relational database. It indexes well, and it supports the kind of custom validation users will want — a text attribute capped at 10 characters, a number attribute restricted to a range like 24–42 — because the type and its rules are themselves just relational data. In a document store, all of this would be a fight.

**Permissions.** In almost every company we have worked at, permission management was implemented in a relational database. It is a solved problem there. We had no reason to expect Boards to be different, and good reason to expect relational to handle it cleanly.

Put together, every hard case pointed the same way: a relational database was not just adequate, it was the natural fit. PostgreSQL handles all of it, including the parts that looked like they might want something fancier. We will get to exactly how the attributes work when that feature enters the series.

If you want to skip ahead to the result, the actual EF Core models — how all of this ended up structured once it was built — live in the [DataAccess/Models folder](https://github.com/win7user10/Laraue.Apps.Boards/tree/main/src/Laraue.Apps.Boards.DataAccess/Models) of the backend repository. What is described above is the early thinking; that folder is where it landed after the schema evolved through the rest of the series.

## Frontend: Nuxt 4 and Vue 3

Vue 3 is the framework we always reach for on the frontend — the same "use what you know" logic as the backend. TypeScript throughout, for the type safety that keeps a solo project from drifting.

Nuxt 4 on top of Vue was the one choice driven by future requirements rather than current ones. We also had recent first-hand experience with it: this blog is built on Nuxt 4, and the feeling was consistently good — whatever functionality we needed was already there, either as a core part of the framework or as an existing plugin. That track record matters for a stable-technology choice. A framework where the common needs are already solved is one you fight far less.

None of the following are needed today, but choosing a framework that supports them costs nothing now and avoids a painful migration later:

- **i18n** — the app ships with Russian and English from the start, and that is a deliberate audience choice. The Russian-speaking audience is one of the largest on Telegram, and it is our native language, so supporting it well is natural and important for us. English is the default for everyone else. Two languages from day one means localisation has to be built in rather than bolted on later, and Nuxt's i18n keeps it manageable as the number of pages grows.
- **SSR** — this one is a deliberate growth bet, not just a technical nicety. We may let users make boards public in the future: if someone wants to share their progress on something, they make that board's URL public. Boards are full of text — issue titles, descriptions, the content of the work itself. If those public boards are server-rendered and indexed by search engines, that user-generated content can show up in search results and bring in new people who land on a real board and discover the product through it. It is a way to turn the content users create (with their permission) into organic traffic. That only works with server-side rendering, and retrofitting SSR onto an SPA that was not built for it is painful — so the framework that supports it had to be chosen now, even though the feature is far off.
- **SEO** — the same reasoning applied to any shared or public link: it would want proper meta tags and server-rendered content so it represents well when shared or indexed, and Nuxt handles that cleanly.

The biggest of these is the eventual move from SPA to MPA, and the reasoning behind deferring it is worth spelling out.

A task tracker is much more convenient when any link inside it can be shared — a board, a specific issue, a filtered view. That means real, separate URLs for pages, not a single SPA route with everything behind client-side state. But designing good, stable URLs takes time, and on an app that is still changing constantly, a lot of that work can turn out wasted: you carefully design a URL scheme, then the feature it described changes shape and the scheme no longer fits.

So we made a sequencing decision. Ship a stable app first as an SPA, get the product right, and only then improve the experience by moving pages onto their own shareable URLs — once the shape of those pages has stopped moving. For that to be painless when the time comes, we needed a framework that makes the SPA-to-MPA transition easy. Nuxt is that framework. We picked it for a migration we have deliberately not done yet.

## The thread running through all of it

Every one of these choices is boring, and every one is deliberate. The backend is the language we know best. The database is the one we trust, chosen specifically because a past project taught us what happens when you pick for excitement instead. The frontend is the framework we always use, on top of a meta-framework chosen for where the product is going.

The exciting part of a project should be the product. The stack should be the part you do not have to think about — because it is stable, familiar, and proven. The MongoDB rewrite cost us a whole project's worth of migration to learn that. We would rather not relearn it.

## What comes next

With the stack chosen, the work finally becomes code. The next article is the first iteration of the backend — a single Telegram host, the first schema, and the CI pipeline — set up before anything is deployed.