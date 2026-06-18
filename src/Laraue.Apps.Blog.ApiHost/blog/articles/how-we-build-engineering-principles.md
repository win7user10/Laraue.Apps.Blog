---
title: How we build — engineering principles for shipping real software
description: The engineering principles behind Laraue projects — starting from user paths, balancing development speed and architecture correctness. When to add database indexes and to write tests. Where AI helps and where it doesn't belong.
type: article
createdAt: 2026-06-18
updatedAt: 2026-06-18
tags: [engineering, architecture, testing, ai-workflow, database, development]
---

This article is about how we engineer a product once the decision to build it has been made. It is one half of how we work at Laraue. The other half — how we decide what to build in the first place — is covered in [How we decide what to build](how-we-decide-what-to-build).

We link here from individual project series rather than repeating ourselves in each one.

---

## Start from the user path, not the schema

Before writing a single line of code, we map the sequence of actions a user takes to accomplish something. How many steps does it require? Where does it feel like friction? Can any step be eliminated or combined?

Only once that sequence feels right do we start designing the data model. The schema serves the user path — not the other way around. An elegant database design that produces a clunky user experience is a failure.

This also means the schema is allowed to be minimal at first. We build exactly what the current user path requires and nothing more. Features that are not yet in the user path do not get tables.

## Balance speed and correctness — and accept that the balance shifts

Early in a feature's life, the goal is to find out whether the approach is right. We implement quickly, even imperfectly, and ship it. If the implementation does not hold — if it is hard to extend, confusing to work with, or wrong in ways we did not anticipate — we refactor.

Refactoring a working feature is easier than designing it perfectly upfront, because by the time you refactor, you understand the problem. Design documents written before implementation are guesses. Code written after usage is knowledge.

There is no universal rule here. Sometimes writing something is better than writing nothing; sometimes the opposite. The judgement is about which situation you are in. When nobody — including you — understands how to approach a task, writing something, even badly, is the fastest way to turn a vague problem into a concrete one you can react to. But when the thinking is still flowing — when you can hold the problem in your head, sketch it on a desk, talk it through — that is cheaper than committing it to code. A wrong idea is far easier to throw away as a sketch than as a branch. So the rule is: think on paper and in conversation while the stream of thought is still running, and reach for code only when thinking alone stops making progress.

The balance shifts over time. Early iterations move fast and cut corners deliberately. As a feature stabilises and other things depend on it, correctness takes priority. The goal is to know which stage you are in.

## Add indexes when writing the logic, not when designing the model

Indexes are a consequence of how the application queries data, not a property of the data itself. So we add them while working on the services that read and write the data — not at the model design stage.

At the model stage, you are guessing which columns will be filtered, sorted, or constrained. Those guesses are usually wrong, because the access patterns are not yet known. Once the service logic exists, the patterns are explicit: this query filters by space and status, that one sorts by creation date, this column must be unique within an organization. The indexes follow directly from that.

An index that does not match a real query is dead weight — it slows down writes and occupies space without speeding up any read. Adding indexes alongside the logic they support keeps every index tied to a concrete access pattern.

## Keep dependencies minimal

Every third-party library is a liability as much as an asset. Libraries go abandoned. They change their licences. They receive breaking changes on their own schedule, not ours. Any of these can force unplanned work at the worst possible time.

Where we can implement something ourselves at reasonable cost, we do. Where a dependency saves significant time and we understand it well enough to debug it, we use it. The question we ask before adding a dependency is not "does this solve the problem" but "what does it cost us if this library disappears in two years."

This applies to frameworks as much as libraries. We prefer technologies with long track records and stable APIs. A frontend framework that has been consistent for five years is worth more than one with better benchmarks and a history of major breaking releases.

## Write tests after stability, not before

Writing tests for a feature that is still changing its shape is wasteful. The tests get rewritten every time the schema changes, which is often in the early stages of any feature. We do not chase coverage numbers.

Tests arrive when a feature stabilises — when the data model has not changed in weeks, the API contracts are settled, and other features depend on this one. At that point, tests written now will stay relevant.

We do not aim for full coverage. We write one or two tests per action. For a list endpoint with many filters, we test that one or two filters work — not every combination. The remaining cases get tests only when a bug actually appears in them.

The reason is cost. Test support is expensive, and that cost is paid every time the code changes, not once. A test suite that covers every edge case upfront becomes a tax on every future change, most of it spent maintaining tests for cases that never break.

This is also why we spend significant time on test architecture when writing the first tests for a project. Tests are often near-copies of each other, one per case. If the structure is hard to read or carries a lot of boilerplate, that cost is multiplied across every test that follows. Getting the test structure right early — readable, minimal boilerplate, easy to copy for the next case — is what keeps the long-term support cost manageable.

When a bug surfaces, we add a test that reproduces it before fixing it. This prevents the bug from returning silently and builds a test suite that reflects real problems rather than imagined ones.

## The AI workflow — where it helps and where it does not touch the codebase

We use AI in three specific places during development. Each one addresses a real gap that comes from being a small team of backend engineers building full products.

**User path validation.** Before implementing a feature, we describe the user's goal to Claude and ask it to map out what the user path should look like — how many steps, where the friction is, what could be simplified. A backend engineer can design a correct data model for a feature and still produce a confusing interface. AI fills the product-thinking gap without requiring a dedicated product manager.

**Market research.** Before committing to a direction, we ask Claude to investigate what already exists in the space — what tools cover it, what they miss, and whether the gap we see is real. This is the kind of research that is easy to skip when you are in a hurry to build, and easy to regret later.

**Frontend prototyping.** We ask Claude to produce HTML prototypes of new screens — single files, iteratable in a chat conversation with mock data. We work through the layout and interactions in the prototype until the experience feels right. Only then do we split the HTML into real components and wire them to real API data. The prototype is never the deliverable. It is a fast way to see whether a layout idea works before writing production code.

### Where AI does not touch the codebase: backend data logic

The backend handles data that belongs to real users. A mistake in a service method, a wrong migration, a flawed permission check — any of these can corrupt or expose data across every user in the system. The consequences are not limited to the person running the code.

AI-generated code can be subtly wrong in ways that are not immediately visible. It may produce code that passes a quick review, works in the happy path, and fails quietly in an edge case that only appears under real usage. For frontend components, a rendering bug is visible and recoverable. For backend data logic, a bug may be silent for weeks and expensive to undo.

We review and write all backend code ourselves. AI is used to understand problems, research options, and discuss approaches — not to write the implementation.

### Reviewing AI code can cost more than writing it

There is a common assumption that AI generation is always faster. For backend logic, it often is not. Reading code someone else wrote — and an AI is, effectively, someone else — and verifying it handles every edge case is cognitively harder than writing it yourself, where you hold the full intent in your head as you go.

AI-generated code tends to look correct. It compiles, it passes the obvious case, and it reads cleanly. That surface plausibility is exactly the problem: it invites a shallow review. To actually trust it, you have to reconstruct the reasoning the AI never explained, check the edge cases it may have skipped, and confirm it fits the existing architecture rather than introducing a parallel pattern. By the time that review is thorough enough to trust, the time saved during generation is usually gone.

There is also a difference in where the mistakes hide. When a human writes code, the errors cluster where the problem is hard — the tricky edge case, the concurrency corner, the part the author themselves found confusing. A reviewer can lean on that: read the easy parts quickly, slow down on the hard ones. With AI-generated code, that heuristic breaks. The mistakes are effectively random. A function can handle the genuinely difficult case correctly and then get a trivial comparison backwards, because the model has no sense of which parts are hard and which are obvious. That means you cannot allocate attention by difficulty. Every line needs the same scrutiny, which is slower and more tiring than reviewing a colleague's work.

So for the parts of the system where correctness matters most, we write the code. The speed argument does not hold there once review is counted honestly.

### Using AI without letting your skills erode

A tool that does the thinking for you erodes the ability to think. If every problem is handed to an AI, the muscle that solves problems independently weakens — and that muscle is exactly what you need to catch the moment the AI is confidently wrong. The dependency becomes circular: you need the skill to check the tool, but leaning on the tool erodes the skill.

This is why the boundary matters in both directions. AI handles research, exploration, and prototyping — work that informs our thinking without replacing it. The core engineering, the decisions that require deep understanding of the system, stays with us. We stay sharp by continuing to do the hard parts ourselves, and we use AI to remove the drudgery around them, not the substance.

The developer remains accountable for every line that ships. A tool cannot hold that accountability, so it cannot be allowed to make the decisions that accountability rests on.

---

These principles are not fixed. They have changed as our projects have grown and as we have made mistakes that taught us something. The specific mistakes are documented in the project series where they happened. And before any of this engineering begins, there is the question of what to build at all — covered in [How we decide what to build](how-we-decide-what-to-build).