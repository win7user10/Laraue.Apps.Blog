---
title: Epics — organizing issues into boards
description: An Epic in Laraue Boards is a kanban board with custom status columns. Learn how to create boards, configure columns, sort and filter issues, and use drag-and-drop.
keywords: [kanban board telegram, epic project management, custom board columns, task board telegram, kanban columns custom]
type: documentation
project: boards
order: 2
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
An Epic is a kanban board — a named collection of status columns that group related issues together. In Laraue Boards the terms "board" and "epic" are used interchangeably. Each epic has a short key prefix (like `WRK` or `MOB`) that appears in issue keys.

## Creating an epic

Tap the **+** button in the bottom corner and choose **New board**. Fill in:

- **Name** — the display name, e.g. "Sprint 1" or "Bug Fixes"
- **Key** — a 2–4 character prefix for issue keys, auto-generated from the name
- **Color** — used for the dot in the navigation tabs and board summary cards

By default every new epic gets three columns: **To Do**, **In Progress**, and **Done**.

## Status columns

Each column in a board represents a status. Issues move from left to right as work progresses. You can:

- **Add a column** — tap **+ Add column** at the right side of the board
- **Edit a column** — tap the edit icon in the column header to rename it or change its color
- **Delete a column** — issues in a deleted column move to the first remaining column, or to the Backlog if no columns remain
- **Reorder columns** — drag columns by their header handle

There is no limit on the number of columns. A simple workflow might have three; a more complex one might have seven or eight.

## Sorting and filtering issues

Each board has controls in the header for sorting and filtering:

**Sort by:**
- Manual — the default drag-and-drop order
- Time — by message timestamp, newest first
- Sender — alphabetically by sender name

**Filter by attribute** — if you have added custom attributes to the board, filter chips appear in the filter bar. Tap a chip to show only issues with that attribute value.

**Board search** — tap the search icon in the board header. Type any text and the board instantly shows only matching cards across all columns. The search clears when you navigate away.

## Drag and drop

Cards can be dragged between columns but not reordered within a column (order within a column is controlled by the sort setting). Column reordering is done by dragging the column header.

On touch screens, press and hold a card for a moment before dragging to distinguish a drag from a scroll.

## The board summary

When your Backlog is empty (all issues are assigned), the Backlog view shows a summary grid of all your boards. Each board card shows:

- A segmented progress bar colored by status — grey for To Do, orange for In Progress, green for Done
- The count of done vs total issues
- Active, to-do, and done breakdowns

Tap any board card to navigate directly to that board.

## Deleting an epic

Open the board and tap the **⋮** menu, then choose **Delete board**. All issues are moved back to the Backlog. This cannot be undone.

## Related pages

- [Issues — turning messages into tasks](/blog/documentation/laraue-boards/concepts/issues)
- [Spaces — grouping boards by project](/blog/documentation/laraue-boards/concepts/spaces)
- [Custom attributes](/blog/documentation/laraue-boards/features/attributes)
