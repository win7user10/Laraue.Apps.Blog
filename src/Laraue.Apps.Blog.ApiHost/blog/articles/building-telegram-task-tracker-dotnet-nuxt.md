---
title: Building a Jira alternative solo — why we built it, and the repositories
description: Part 1 of a series on building a Telegram-native task tracker solo and AI-assisted. The problem we set out to solve, why another task tracker makes sense, what is live today, and the two repositories the series is built on.
type: article
createdAt: 2026-06-19
updatedAt: 2026-06-19
projects: [boards]
tags: [dotnet, nuxt, telegram, task-tracker, devlog, architecture]
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 1.
> This is the opening article in a series documenting how [Laraue Boards](../projects/boards) was built: the decisions, the trade-offs, and the mistakes. At the end of each build stage you get the next working version of the app. If you want to know what the product *is* rather than how it was built, see the [project page](../projects/boards) and the [documentation](../documentation/laraue-boards).

This first article is only about *why*. Why build another task tracker at all, what the product is meant to be, what already works, and where the code lives. The engineering starts in the next articles.

## Why another task tracker makes sense

There are already many task trackers, and a new one cannot compete with Jira or Linear on features, integrations, or brand. That is not the goal, and pretending otherwise would be the fastest way to waste a year. We knew the niche was crowded before writing a line of code — that decision was deliberate, not a discovery made after launch. (We wrote separately about [how we decide what to build](how-we-decide-what-to-build) and why building consciously in a crowded market is a valid choice.)

What makes the effort worthwhile is a single focus: deep Telegram integration — deeper than anything that currently exists. Most small teams and solo developers already live in Telegram. They communicate there all day. But the moment a task needs to be tracked, the workflow breaks: open a browser, log into a tool, find the right board, create an issue. The context switch is small but it compounds. Things get written in Telegram and then forgotten.

The immediate version of Laraue Boards closes that gap at its simplest: you send a message to a bot, and it becomes a task. You open a web app to organise those tasks on a board. No tool is built specifically for people who already live in Telegram. That is the space Laraue Boards is trying to occupy.

## Where the product is heading

The current version is deliberately simple, but the direction is not.

The plan is for the bot to become a full participant in any Telegram chat. A team member mentions the bot in a conversation, the bot creates an issue from that message, and a link to the issue appears in the chat. The issue links back to the original message. The context of why something was created stays attached to the conversation that produced it, instead of being copied into a separate system and losing its thread.

Further out, once the core features are stable, the plan is to bring AI directly into the workflow. A user selects a set of issues as context — all issues in a space from January to March, for example — and asks a question: summarise what was completed this quarter, generate a report in a specific format, identify what is blocked. The issues become the context for the conversation. This kind of AI-assisted reporting only becomes natural when chat and task management live in the same place, which is exactly the bet this product makes.

## What is live today

Laraue Boards is live at [msgboard.laraue.com](https://msgboard.laraue.com), and the core loop works end to end:

- A **Telegram bot** captures messages and stores them as tasks.
- **Kanban boards in a Telegram Mini App**, opened directly from the bot.
- A **web version** with authentication via Telegram — no password required.
- **Organization mode** with a permission model for multiple users.

The system is early and has rough edges. But the loop that matters — capture a task in Telegram, see it on a board, manage it from the web — is real and working. The [documentation](../documentation/laraue-boards) covers the concepts and how to use each piece.

## How the vision already shifted

It is worth being honest about one thing up front, because it shaped the architecture more than any single technical decision.

The original plan was a Telegram Mini App as the *primary* interface. The bot would capture messages, and everything else would happen inside Telegram. In practice, the Mini App turned out to be less convenient than expected — the viewport is limited, navigation differs from the web, and interactions that feel natural in a browser feel awkward inside the app. After using it ourselves, the web application became the main interface, and the bot settled into the one job it does better than anything else: capturing a task without leaving the chat.

That was the first real mistake of the project — assuming the Mini App would carry the whole experience. It is the first of many the series documents honestly, because the mistakes are usually where the useful information is.

## The repositories

The series is built on two public repositories:

- **Backend** — [Laraue.Apps.Boards](https://github.com/win7user10/Laraue.Apps.Boards) — .NET 10 / C#, PostgreSQL 18
- **Frontend** — [laraue-boards](https://github.com/win7user10/laraue-boards) — Nuxt 4, Vue 3, TypeScript

They are kept separate on purpose. Backend and frontend have independent release cycles — a fix or a feature on one side should not require touching, testing, and deploying the other. Separate repositories make that boundary explicit and keep each deployment pipeline clean.

Following along with the code as the series progresses is the most direct way to see how each decision plays out. Where a choice has a story behind it — and most do — the article will tell it.

## What comes next

The next article is about something that happens before any backend or frontend code exists: prototyping the interface. You cannot choose a stack, design a schema, or define a data model without first knowing what the product looks like and what the user actually does with it. That is where the real work starts.

The principles underneath all of this — how we balance speed and correctness, when we write tests, and where AI is allowed to help — are described separately in [how we build](how-we-build-engineering-principles), so the series articles can stay focused on the actual build.