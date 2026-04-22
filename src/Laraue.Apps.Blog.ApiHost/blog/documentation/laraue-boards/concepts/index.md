---
title: How Laraue Boards works
description: The key concepts in Laraue Boards: Issues, Epics, Spaces, and Organizations. Understand the hierarchy before diving into features.
keywords: [laraue boards concepts, issue epic space organization, project management hierarchy telegram, how laraue boards works]
type: sectionDefinition
order: 2
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
Before using Laraue Boards day-to-day, it helps to understand how the pieces fit together. The hierarchy is simple: issues live inside epics, epics live inside spaces, and spaces belong to an organization.

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
              └── Issue (e.g. WRK-42)
```

Each level is optional. A solo user can work with just epics and issues, no spaces or organization needed.
