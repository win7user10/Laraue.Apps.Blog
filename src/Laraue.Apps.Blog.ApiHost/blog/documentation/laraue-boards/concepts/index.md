---
title: How Laraue Boards works
description: The key concepts in Laraue Boards - Issues, Epics, Spaces, and Organizations. Understand the hierarchy before diving into features.
keywords: [laraue boards concepts, issue epic space organization, project management hierarchy telegram, how laraue boards works]
type: sectionDefinition
order: 2
createdAt: 2026-04-22
updatedAt: 2026-08-04
---
Before using Laraue Boards day-to-day, it helps to understand how the pieces fit together. The hierarchy is simple: issues sit in a status, statuses live inside epics, epics live inside spaces, and spaces belong to an organization.

## In this section

- [Issues](/blog/documentation/laraue-boards/concepts/issues) — The core unit of work. A task captured from a Telegram message or created manually, with a unique key like `WRK-42`.

- [Epics](/blog/documentation/laraue-boards/concepts/epics) — A kanban board with custom status columns. Issues are organized inside epics and move through columns as work progresses.

- [Spaces](/blog/documentation/laraue-boards/concepts/spaces) — A grouping layer above epics. Use spaces to separate client work, products, or departments.

- [Organizations](/blog/documentation/laraue-boards/concepts/organizations) — A shared workspace for a team. Members of an organization can see and collaborate on the same epics and spaces.

## The hierarchy at a glance

```
Organization
  └── Space (e.g. "Mobile App")
        └── Epic / Board (e.g. "Sprint 1")
              └── Status (e.g. "In progress")
                    └── Issue (e.g. WRK-42)
```

The first time you log in, you already have all of this set up — a personal organization with one space and a default board waiting for you. You can just start sending messages to the bot and organizing issues right away. Spaces and epics become useful once you want more structure: create a new space for a separate project, or a new epic for a new board, whenever you need one.