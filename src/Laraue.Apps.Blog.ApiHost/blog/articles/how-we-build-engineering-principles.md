---
title: How we build — engineering principles for working on real products
description: The engineering principles Laraue projects follow — start from the user path, do not overcomplicate ahead of time, split logic into layers, isolate third-party integrations, and where AI helps and where it is not allowed near the codebase.
type: article
createdAt: 2026-06-18
updatedAt: 2026-07-12
tags: [engineering, architecture, testing, ai-workflow, database, development]
nextLink: reviewing-ai-generated-cost
previousLink: how-we-decide-what-to-build
---

We will try to list all the rules we follow at Laraue when building software. We link to this document from individual articles so we do not repeat the principles in each one. Some of the points link to the stories the rule came out of.

## We design the database and write code only once the user path is clear

Before moving on to the database or the code, we write out the sequence of actions a user takes to achieve some result. The cost of fixing a mistake grows several times over with every subsequent stage of development. Fixing a mistake in a mockup takes 2 minutes; rewriting a feature's code takes 2 days. So first we describe the user path, then we stare at it for a long time, trying to find the inconsistencies. We move on to designing the database and writing code last of all.

It follows from this that code is added only in line with the current needs. It is useful for a developer to know which features are planned for the next iterations, so that room for extending the architecture is left in the right places. But adding code or tables that have nothing to do with the current feature to a PR is a direct path to technical debt out of nowhere. How this principle plays out on Laraue Boards is in the article [about the user path](telegram-saved-messages-to-task-tracker).

## Start simple, add complexity only when it is really needed

The ability to do only what is really needed at the current stage is an irreplaceable skill. Without it, infrastructure gets designed for a scale the product may never reach. Developers can optimize SQL queries that produce no load at all, in features nobody uses. There are plenty of examples of wasting time and resources like this in any field.

While building Laraue Boards, every infrastructure decision followed one rule: pick the simplest and cheapest option that satisfies the current needs, and add complexity only when there is a concrete reason. A cloud database has real advantages — backups, failover, point-in-time recovery — but paying for them makes sense when there is something to protect. When you have three active users, a self-hosted option on the same VPS where the main app runs handles the job just fine.

The same logic can be seen in the [deploy article](deploying-dotnet-postgres-vps-docker-compose): media can perfectly well be stored on the VPS file system instead of S3, and the app's migrations can be run at app startup instead of setting up complex pipelines. Every decision is a trade-off. It cannot be right or wrong on its own. What is a standard for big companies is, for small ones, a path to wasted resources or bankruptcy.

## We use a layered architecture for services — but only where it is needed

We move the logic that should not depend on the app's entry point into the Core services project (`Services`), and the host-specific logic into the host services (`HostNameServices`). A **Core service** can perform the operation in the form in which it will be correct regardless of the caller: creating an issue must add a record to the change history whether the request came through the Telegram bot or the web API. A **Host service** contains only the host-specific logic: permission checks, validation, transaction management. After performing such actions the request is usually forwarded to the Core service. The approach is covered in detail in the [backend architecture article](clean-dotnet-telegram-bot-architecture).

It is important to understand why the rule exists: the split helps reuse code across different hosts, avoiding duplicated logic. If the functionality is limited to one host — for example, the whole project is a host for a Telegram bot — the split may be redundant and will only lead to a more complicated project and time spent maintaining an architecture that does not help with anything.

## We do not let integration models leak into the app's core

When an app depends on an external service, that service's types must not leak inside the domain. Storing a third-party `Telegram.Bot.Types.Message` model in your own database is strongly discouraged.

The right approach is to define your own models and DTOs for any service operations and map the third-party types into them. For example, the Telegram host watches for new messages in the chat (`Telegram.Bot.Types.Message`), maps them into a local DTO (`DomainName.Services.SaveMessageRequest`), and works with that DTO from then on as if Telegram never existed, calling `coreService.SaveMessage(SaveMessageRequest request)`.

Since the domain does not know the integration exists, adding a new one (saving a message from another chat app, for example) will require adding only a new integration layer, rather than rewriting a core service that is already stable.

## We balance development speed and architectural correctness — and accept that the balance can shift

When building new functionality, it is impossible to guess for sure whether users will like it. So the goal is a fast, even if not perfect, implementation of the feature, and getting it in front of users. Then we watch the results. If the feature turns out to be unpopular, we can skip spending time on optimizations. If users do like it, we set aside time for refactoring, so that we can start improving it afterwards.

Early iterations are fast and imperfect in terms of code; they exist to check demand. Later stages, on the contrary, are not as sensitive to speed, but demand maximum stability of the functionality.

## We add indexes when writing the logic, not when designing the model

Indexes are a consequence of how the application queries the data, not of the data itself. So we add them while working on the services' functionality, not while designing the database. At the modelling stage you can only guess which columns will be filtered or sorted on. But while writing the service it is already clear that the query filters by status and sorts by creation date. Hence the rule: create indexes for concrete cases, rather than trying to invent them at the database design stage.

## Optimising a query matters orders of magnitude more than optimising code

In web applications the cause of a slow server response is almost always in the database queries, not in the code. A single badly-shaped query — a missing index, an N+1, a join pulling more than it needs — can increase the total response processing time by orders of magnitude. So when there are problems with response time, the queries are the first suspect.

This does not mean the application code can be written any old way. A loop with O(n²) over a very large collection can easily make the server think hard. But micro-optimisations — replacing the division `x / 2` with `x >> 1`, for instance — bring no benefit at the scale of a large application and only make the code harder to read.

## We keep dependencies to a minimum and prefer stable technologies

Every third-party library is a potential uncertainty for the product. Libraries get abandoned, change their licences, and break compatibility whenever they see fit. So unplanned work can appear at the worst possible moment — for example, vulnerabilities turn up in the version of a library the product was using, while the newer versions already contain breaking changes that would force half the project to be rewritten. So it is always worth weighing the risks and deciding: write the code yourself, or use a ready-made solution.

This applies not only to libraries, but to frameworks and databases too. Stable technologies have documented limitations, and they often get criticised for something. But they are proven by time, and there is no fear of hearing one day that the maintainer has completely rethought the approach and decided to rewrite everything, breaking compatibility. A concrete example of how we chose the stack for Laraue Boards is in the story [about choosing the stack](choosing-stack-for-solo-project).

## We write tests after the product stabilises, not before

Writing tests for a feature when nobody yet knows what final shape it will take is wasteful. The TDD methodology assumes an ideal world in which the business knows exactly how the functionality being built should work. The fact is that until the product is tried by test or real users, this is not known.

We are for tests. But against writing them to satisfy metrics. Our flow looks like this: the developer writes the code and demonstrates it on a test environment. Based on the feedback, the requirements get adjusted. The developer covers the positive and critical scenarios with tests. If problems are found during testing, they are fixed through tests.

Supporting tests is very expensive: writing tests that are easy to read and modify is within the reach of very few developers. And given their tendency to be written by copy-paste, and the lack of a close look at them during review, tests turn into an enormous unmaintainable wall of code far more often than the main code does.

## There are always exceptions to the rules

All the principles above are experience gained while working on projects. The important thing is not to follow them blindly, but to understand the conditions they arose under and whether they apply to the current situation.