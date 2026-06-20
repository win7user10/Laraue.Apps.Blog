---
title: From Telegram Saved Messages to a real task tracker — defining the user path
description: Part 3 of building a Telegram task tracker solo. Why Saved Messages fails as a to-do list, the minimal single-user flow Laraue Boards started from, and why the user path is defined before any schema.
type: article
createdAt: 2026-06-20
updatedAt: 2026-06-20
projects: [boards]
tags: [dotnet, telegram, saved-messages, product, user-path, devlog]
previousLink: prototyping-ui-with-ai-before-code
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 3.
> The [previous article](prototyping-ui-with-ai-before-code) was about prototyping the interface. Now there is something concrete to point at — and the next question is what the user actually does with it.

A prototype shows what the product looks like. It does not tell you what the user does, in what order, or where the flow should start and stop. That is the next thing to pin down, and it has to be pinned down before any schema exists — because the schema is built to serve the path, not the other way around. We wrote about why we [start from the user path](how-we-build-engineering-principles) as a general principle; this article is the specific path Laraue Boards began with.

## Start with the smallest possible scope

The first version is single-user. No organizations, no teams, no sharing, no permissions. One person, their own tasks, nothing else.

This is deliberate. Complexity grows fast once multiple users are involved — who can see what, who can edit what, what happens when two people touch the same thing. Starting single-user removes all of that and leaves only the core question: does the basic loop of capturing and organising tasks actually work and feel good? Everything multi-user gets built on top of this foundation later, once the foundation is proven.

## The first user path

The path the first version supports is small enough to write in two lines:

- The user writes a message to the bot → a card is added to the backlog.
- The user opens the app → they manage cards on a kanban board.

That is the whole thing. Two actions, two surfaces, one user.

The first half is capture. Instead of writing a task into Telegram's built-in Saved Messages — where it sits in an undifferentiated pile and gets forgotten — the user sends it to the bot. The bot stores it as a card. Capture stays exactly where the user already is, inside Telegram, with no context switch.

The second half is management. When the user wants to actually organise their tasks — move them between columns, set their status, see the whole board — they open the web app. This is the work that does not fit inside a chat, and trying to force it into one is a mistake we made and corrected later in the series.

## Why the bot instead of Telegram Saved Messages

Telegram already has a place to send yourself notes: Saved Messages. Most people use it as an informal to-do list. So why build a bot that does what Saved Messages already does?

This was worth checking rather than assuming, so we asked around. All of our friends are Telegram users, and we asked them two things: do you use Telegram's Saved Messages as a notebook that is always within reach, and do you later run into trouble sorting through what you saved? The answer to both was yes, across the board. They are active Saved Messages users precisely because Telegram is always there — on the phone, on the computer, one tap away. It is the most convenient place to dump a thought. But it was never designed for classifying or organising those thoughts afterwards, and that is exactly where it falls down for them.

That gap is the opening. Saved Messages has picked up some organisation features — pins, and emoji tags that let you label and filter what you have saved. But tagging a pile is still a pile. There is no status on a message, no board, no columns, no way to move something from "to do" to "in progress" to "done," no notion of a task that can be worked through a process. You can label a note, but you cannot manage it. A message goes in and, tags or not, nothing happens to it unless you go back and act on it manually. Piles do not get managed — they get forgotten.

The bot looks similar at the moment of capture — you send a message, just like Saved Messages — but what is captured becomes a real entity in a system that can organise it: a card with a status, on a board, that moves through a workflow. The capture is as frictionless as Saved Messages; everything after capture is what Saved Messages, even with tags, was never built to do.

That contrast — same easy capture, but the message becomes something you can actually work with — is the core of the whole product, and the first user path is the smallest version of it that delivers real value.

## How the path informs everything after it

Writing the path out before any code does real work. It tells you what the data model has to support: a user, a card, a backlog, a board with columns, a status on each card. It tells you what the bot has to do (receive a message, create a card) and what the web app has to do (read cards, move them, change status). Nothing more — and equally important, nothing the path does not call for.

This is why the path comes before the schema. The schema in the next article is not designed from first principles or from what a task tracker "should" have. It is designed to support exactly these two actions, and to stop there.

## A note on what we deferred

There is one decision worth flagging now, because it became a mistake later: we did not think hard enough about the multi-user path at this stage. Single-user-first is the right call, but it is different from single-user-only. Some schema decisions made for one user turned out to need rework once organizations arrived, because the multi-user shape was never sketched even loosely. Starting minimal is correct; pretending the later stages do not exist is not. We will come back to exactly what that cost when organizations enter the series.

## What comes next

The path defines what the data has to do. The next step is the data model itself — the first, deliberately minimal schema, designed to support this single-user flow and nothing more.