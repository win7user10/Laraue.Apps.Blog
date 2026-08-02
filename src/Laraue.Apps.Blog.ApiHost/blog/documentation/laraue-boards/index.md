---
title: Laraue Boards - Task Management system
description: Laraue Boards turns your Telegram messages into structured tasks on a kanban board. No migration, no new habits, works as a Telegram Mini App and in the browser.
type: rootSectionDefinition
icon: 📋
createdAt: 2026-04-22
updatedAt: 2026-08-01
---

Your team already works in Telegram. Decisions happen there, tasks get assigned there, files get shared there. Laraue Boards connects directly to that workflow — turning the messages you already send into structured tasks on a kanban board, without asking you to change how you communicate.

![A Laraue Boards kanban board with issues organized into status columns](https://laraue.com/static/images/blog/docs/laraue-boards/board-example.jpg)

## What Laraue Boards is

Laraue Boards is a project management tool built around a single observation: **the task already exists the moment someone sends a message**. You just need a way to organize it.

- Open it as a **Telegram Mini App** — no separate login, no new account
- Or use the full **web app** at [boards.laraue.com](https://boards.laraue.com)
- Send a message to the bot — text, photo, video, or an album — and it becomes an issue automatically, no extra steps
- Edit the message in Telegram, and the issue updates with it
- Organize issues into **Epics** (boards) with custom status columns
- Group epics into **Spaces** for different projects or clients
- Set up **custom attributes** for issues — your own fields for your own process
- Collaborate with your team through **Organizations**, with permissions configurable per operation

![Configuring per-operation permissions for an organization in Laraue Boards](https://laraue.com/static/images/blog/docs/laraue-boards/permissions-setup-example.jpg)

## Core concepts

| Concept              | What it is                                                                                                            |
|----------------------|-----------------------------------------------------------------------------------------------------------------------|
| **Issue**            | A single task, captured from a Telegram message or created manually. Has a unique key within its space, like `WRK-42` |
| **Epic**             | A board with status columns — your kanban board                                                                       |
| **Space**            | A group of related epics, like a project or client                                                                    |
| **Organization**     | A shared workspace for your team, with permissions configurable for each member                                       |
| **Backlog**          | The default epic issues land in until you sort them onto your own boards                                              |
| **Custom attribute** | A field you define for issues — like a type or a priority — set up by an admin                                        |

## Who it's for

**Solo users** — use it as a personal task inbox. Telegram messages become your backlog, boards become your workflow. No team required.

**Small teams** — create an organization, invite teammates via a link, and share boards. Works especially well for teams in CIS countries where Telegram is the primary business communication tool.

**Agencies** — separate client work into spaces, track what's been discussed and what's been delivered, without buying a separate PM tool for each client.

## Where to go next

- [Create your first board in 5 minutes](/blog/documentation/laraue-boards/getting-started/quick-start)
- [Logging in with Telegram](/blog/documentation/laraue-boards/getting-started/authorization)
- [Understanding Issues](/blog/documentation/laraue-boards/concepts/issues)