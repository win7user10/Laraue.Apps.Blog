---
title: How we decide what to build — validating a product idea in a crowded market
description: How we validate a product idea before writing code — researching a crowded market, testing the idea in public, and using AI to judge whether we can realistically compete.
type: article
createdAt: 2026-06-18
updatedAt: 2026-06-18
tags: [product, validation, indie-hacking, market-research, crowded-market, ai-workflow]
---

This article is about the part of building software that happens before any code exists: how to decide whether and what to build, consciously, and how to validate a product idea before writing code. It is one half of how we work at Laraue. The other half — how we engineer a product once that decision is made — is covered in [How we build — engineering principles](/blog/articles/how-we-build-engineering-principles).

We link here from individual project series rather than repeating ourselves in each one.

---

## Research the market before building anything

The worst outcome in product building is to spend months convinced you are going to change the world, ship, and only then discover the market is full of better alternatives you never looked for. That is avoidable. The fix is to research the space before committing, so that whatever you decide to build, you decide it consciously.

Conscious does not mean optimistic. When we started building a task tracker, we understood the niche was crowded and that our product would never sit at the top of search results for "task tracker." That was a deliberate, eyes-open decision — not a discovery made after launch. Building a product in a crowded market is a valid choice; building one without knowing the market is crowded is not. Knowing the constraint upfront changes what you build and how you position it. Discovering it afterward just wastes the months in between.

So before building, look honestly at who else is in the space, what they do well, and where the real gap is. The goal is not to be discouraged out of building — it is to build with a clear understanding of where you actually stand.

## Test your idea in public to get feedback before building

You do not need a working product to test whether anyone wants it. Build a prototype of the interface, record a short GIF of it, and post it somewhere with a fast, reactive audience — Threads, X, a relevant subreddit, a community in your niche. The reactions tell you something real before you have spent any engineering time.

A prototype and a GIF cost an afternoon. A built feature costs weeks. Putting the cheap version in front of people first is the highest-leverage validation step available, and most builders skip it because showing an unfinished idea feels uncomfortable.

The first time is genuinely scary — but not for the reason you expect. The fear is not that people will react badly. It is that nobody will react at all. A post that lands with total silence feels worse than criticism. If that happens, it is not a verdict on the idea; it usually means the post itself did not communicate well. Rewrite it — or ask AI to rewrite it — and try again. Repeat until you get a response.

Because the response is the entire point. The goal is feedback, good or bad. Bad feedback is still information. And the most valuable reactions are often questions you never thought to ask yourself: someone replies with "but how would this handle X," and you realise X is a problem you have no answer for. Sometimes that single question tells you the build will be far more problematic than you assumed — which is exactly the kind of thing you want to learn now, for the price of a post, rather than three months into development.

This is not theoretical. When we first posted about the [Laraue Boards](../projects/boards) concept, the first four posts got nothing — no reactions, no comments. The fifth landed and drew around forty comments. The questions in that thread were exactly the ones worth hearing early: why is this better than Telegram's built-in Saved Messages? Someone said they would not want to share their data with an unknown publisher. Someone else asked us to make it open source. None of that was discouraging — it was a map of the objections and expectations we would have to address, delivered before we had over-invested in assumptions. And it only took five attempts at writing the post to get there.

## Build the landing page early — and use it to check if you can compete

Build the landing page right after you decide to build the product — not as a last step before launch. A landing page forces you to articulate what the product is and who it is for, in plain language, before the codebase locks those answers in.

The notes you already wrote while researching and deciding are the raw material. Much of that thinking can be transformed directly into landing-page copy. Then use AI to optimise it: ask it to improve the page for SEO, and to identify the keywords you can realistically compete for given the competition you already mapped.

This is also a second honesty checkpoint. Ask AI for a frank assessment of your competitive opportunity for those keywords. Sometimes the answer at this stage is that competing is not realistic — and learning that from a landing-page exercise, before building, can save the entire cost of building. A hard "no" delivered early is one of the most valuable things this process can give you.

## Store every discussion about the product

Keep a record of the discussions, decisions, and reasoning behind the product as you go — chat logs, notes, voice memos transcribed, whatever is low-friction enough that you actually do it.

This archive pays off repeatedly. It lets you reconstruct the chronology later: why a decision was made, what alternatives were considered, what constraint forced a particular choice. And it becomes source material. The stored reasoning can be turned into articles, documentation, changelog entries, even the landing page copy. A devlog series is far easier to write when the decisions were recorded as they happened rather than reconstructed from memory months later.

---

Once the decision to build is made consciously, the work becomes engineering. How we approach that — user paths, schema design, testing, and where AI helps and where it doesn't — is covered in [How we build — engineering principles](how-we-build-engineering-principles).