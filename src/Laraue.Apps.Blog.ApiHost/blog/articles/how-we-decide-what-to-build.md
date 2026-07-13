---
title: How we decide what to build — validating a product idea in a crowded market
description: Validating a product idea before writing code — researching the market, testing the idea in public, and using AI to judge whether it is realistic to compete for the keywords you need.
type: article
createdAt: 2026-06-18
updatedAt: 2026-07-13
tags: [product, validation, indie-hacking, market-research, crowded-market, ai-workflow]
nextLink: how-we-build-engineering-principles
---

In this article we will try to list all the steps we take to work out whether an idea is worth implementing, or whether the effort is better spent on something else. What happens if the idea passes the check is covered in the next article, [how we build](how-we-build-engineering-principles).

## Research the market before starting anything

One of the common paths for a young product company or a solo developer is months of building something nobody needs. The alternatives may simply be better in every way. Or the market for the product may not exist at all. To avoid that, you have to study the niche before you start building.

When we started building Laraue Boards, we understood that the niche was crowded and that our product would never sit at the top of search results for "task tracker." But we also understood that this is a giant market — alternatives to Jira keep appearing, and some of them find their customers. Building a product in a crowded market is a valid choice, but it has to be a conscious one.

Before starting, you have to understand what will make you stand out among the competitors. Building just a task tracker is pointless — Jira wins. But building a task tracker for running a zoo is already far more specific, and it can work, provided the market exists and the competitors do not.

## Find the competitors through search queries or AI

Once you have an idea of what you plan to build, you can research the products that already exist, using search engines or AI. We search for task trackers for running a zoo, and try to estimate how many people look for something like that per month.

If there are no competitors, or we understand that our product can get past them, we can roughly estimate the size of the market for such a product. If every 50th person searching for the product wants to buy it — will that pay back the investment into building it? If yes, we move on to the next part; if not, it is better to look for a different idea.

## Test the idea in public to get early feedback

To find out whether anyone needs the product, you do not have to build it. It is enough to generate a prototype of the interface, record a short GIF, and publish it where there is an audience that reacts — Threads, X, a relevant subreddit. The comments let you draw conclusions before any time has been spent on development.

Creating a prototype and a GIF with AI takes a few hours. A built product takes far longer. Showing people the conceptual version is the most underrated way of validating it, and most people skip it. Showing something unfinished is psychologically uncomfortable — a professional is used to seeing things through to the end.

What matters here is getting at least some feedback. For the audience to react, the post has to ask questions, not just describe the product. Getting no reaction at all is a normal situation: the recommendation systems of these networks are often unpredictable, and the same post can either go viral or get no response whatsoever. So it is important to have a clear goal — for example, to collect 30 comments with feedback. We keep publishing modified versions of the post across different networks until the goal is met.

It does not matter whether the feedback is bad or good; what matters is that it arrived. Users may tell you about the alternatives, asking how this is better than product X. They may confirm or refute that the problem you are solving exists, or say they would use such a product if it had a particular feature.

This is not theory. When we first wrote about the [Laraue Boards](https://msgboard.laraue.com) concept, the first four posts got no response at all. The fifth became popular and collected around forty comments. That feedback made it clear where the product had to go.

## Build the landing page as early as possible — and try to attract search traffic

The landing page is built right after the decision to build the product, not before the launch. Search engines take a long time to index new websites, so this way there is a chance of being in search results by the time the product ships. A landing page also forces you to formulate, in plain words, what is being built and for whom.

The content for the page already exists: you can use the notes and the users' comments gathered during the research and the decision. Most of that thinking can be turned directly into landing page copy. This is what we did: we throw all the information we have about the project into a prompt for the AI and ask it to give us landing page options that are SEO-optimized for our niche.

At this point you can ask the AI to assess the chances of getting organic traffic, or for ways to increase those chances. The AI may suggest SEO copy aimed at a narrower audience, to raise the chances of getting traffic.

Publishing a landing page before a finished product exists is an ordinary practice. As a rule, there are no users at first. And if some do turn up, you can add a "subscribe to updates" button to the landing page, to collect the emails of potential customers.

## Store any textual information about the development process

Chat logs, notes, transcribed voice messages — all of it can come in handy later. Just drop the records into a prompt, and the knowledge base for the AI is ready. It becomes the source material for articles, documentation, changelog entries, and landing page copy. Keeping an archive like this costs almost nothing, but it can bring a lot of benefit.