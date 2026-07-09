---
title: From Telegram Saved Messages to a real task tracker — defining the user path
description: Part 3 of building a Telegram task tracker solo. Why Saved Messages is not convenient for managing tasks, which minimal scenario Laraue Boards started from, and why the user path is defined before development begins.
type: article
createdAt: 2026-06-20 16:20
updatedAt: 2026-07-09 11:00
projects: [boards]
tags: [dotnet, telegram, saved-messages, product, user-path, devlog]
previousLink: prototyping-ui-with-ai-before-code
nextLink: choosing-stack-for-solo-project
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 3.
> The [previous article](prototyping-ui-with-ai-before-code) was about prototyping the interface. Now we have to think through how the user will create issues on the boards directly from Telegram.

The prototype shows clearly how the product will look. But it does not answer how the user's interaction in Telegram with that interface will work. We will try to fix the key points in this article, before the database design stage. We have written about why we always [start development with the user path](how-we-build-engineering-principles) as a general principle; this article is about a concrete example — Laraue Boards.

## The minimal set of features for the first version

We prefer iterative development, where a new version of the app appears at the end of each iteration. If you try to make a fully functional project all at once, there is a risk of never seeing it. So the first version of Boards is single-user. No organizations, no teams, no sharing, no permissions. Just one person and their own tasks.

This is a deliberate choice. Complexity grows quickly once sharing appears: who sees what, who can edit what, what happens when two people change the same thing. Starting with a single-user mode gets us to an MVP faster and lets us check the one thing that matters: is what we are building convenient to use.

## The basic scenario of creating an issue from Telegram

We saw the user path here like this:

- The user writes a message to the bot → a card is added to the backlog → the bot signals success.
- The user opens the app → sees the backlog and manages the kanban boards.

That is about it — just two actions.

The first action is saving. Instead of creating a note in Telegram's Saved Messages, where it lies in a common pile, the user sends it to the bot, and the bot saves it as a card. Saving stays where it already was — inside Telegram.

The second action is managing. When the user wants to work with the cards — assess progress, move them between statuses — they open the web app, which adds the visual component that the chat lacks.

## Why a user needs a bot instead of Saved Messages

Telegram already has a place for notes — Saved Messages. Many people use it as an informal to-do list. So why a bot that does the same thing? The answer: it is not always convenient.

We asked around among friends who are Telegram users, like us. We asked two questions: do you use Saved Messages as a notebook that is always at hand, and do you later have trouble finding your way around those notes? Everyone answered "yes." Telegram is always near — on the phone and on the computer. It is a convenient place to quickly jot down a thought. But it is not designed for classifying and organising those thoughts afterwards.

Saved Messages has emoji tags you can mark and filter saved items with. Some people try to solve the organisation problem by creating several chats. But that does not solve it fully — the set of tags is limited, and information scattered across chats is hard to search.

So a bot can combine the user's familiar way of working with notes with a full system for managing them. On top of that, given Telegram's popularity in small companies in the CIS countries, where they prefer to assign tasks to employees as text in chats, this could evolve into software for small businesses too.

## How the user path defines the data model

The path, written out before any code, lets us understand what the data model has to support: the user, the card, the backlog, the board with columns, a status on each card. It says what the bot has to do (accept a message, create a card) and what the web app has to do (read cards, move them, change status).

The database schema in the next article will be designed not from an abstract set of entities; we understand clearly what has to be added to implement the user scenario.

## What comes next

The next step is designing the data model: it will be a minimal database schema, designed for this specific scenario.