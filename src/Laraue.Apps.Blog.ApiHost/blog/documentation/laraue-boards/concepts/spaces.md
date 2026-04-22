---
title: Spaces — grouping boards
description: Spaces in Laraue Boards let you group related boards together under one label. Use spaces to separate client work, products, or departments without creating separate accounts.
keywords: [project grouping kanban, workspace telegram boards, organize boards by project, multi-project task tracker]
type: documentation
project: boards
order: 3
createdAt: 2026-04-22
updatedAt: 2026-04-22
---

A Space is a label that groups related boards together. If you work on multiple products, clients, or areas, spaces keep your navigation clean by letting you focus on one context at a time.

Spaces are optional — if you only have a few boards and don't need grouping, you can ignore them entirely.

## Creating a space

Tap the **+** button in the bottom corner and choose **New space**, or use the **New space** option inside the space switcher in the navigation breadcrumb.

Fill in:

- **Name** — e.g. "Mobile App", "Marketing", "Client: Acme"
- **Key** — a short identifier used in board listings, e.g. `MOB`
- **Color** — shown as a colored dot throughout the interface

## Assigning boards to a space

When creating a new board, select a space in the assignment step. Existing boards can be moved to a space by editing the board.

Boards without a space assigned are shown under "No space" when filtering.

## Switching spaces

The space switcher appears in the navigation breadcrumb at the top of the screen once you have at least one space. Tap the space name to open the switcher.

The switcher shows:
- **All spaces** — shows all boards regardless of which space they belong to
- Each individual space — filters the navigation tabs and board summary to show only that space's boards

Switching spaces also resets any active board search or sort.

## Editing and deleting a space

Tap the **⋮** icon next to a space name in the switcher to open the actions menu:

- **Edit** — rename the space, change its key or color
- **Delete** — removes the space label. Boards that belonged to this space become unassigned. Issues and cards are not affected.

## Spaces and organizations

In a team context, spaces belong to an organization and are visible to all members of that organization. Individual space-level permissions can be configured — see [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions).

## Navigation sort and filter

When you have many boards across multiple spaces, the board navigation tabs support:

- **Sort** — Manual, Alphabetical, or Last updated
- **Active boards only** — hides boards where all issues are in the last (Done) status
- **Board search** — type in the nav bar to filter board tabs by name

## Related pages

- [Epics — boards within a space](/blog/documentation/laraue-boards/concepts/epics)
- [Organizations — sharing spaces with a team](/blog/documentation/laraue-boards/concepts/organizations)
- [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions)
