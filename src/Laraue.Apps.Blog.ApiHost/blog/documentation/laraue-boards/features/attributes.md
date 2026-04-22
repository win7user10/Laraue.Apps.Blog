---
title: Custom attributes — filter and label your issues
description: Add custom fields to your Laraue Boards issues. Use text, select, or date attributes to label priority, team, due dates, and anything else your workflow needs.
keywords: [custom fields kanban, issue attributes project management, priority field task tracker, custom labels kanban board, filter by attribute kanban]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
Attributes are custom fields attached to every issue on a board. They let you add structured metadata — priority, team, due date, client name, or anything your workflow requires — and filter the board by those values.

## Attribute types

| Type | Use for |
|------|---------|
| **Select** | Fixed list of options — Priority (Low/Medium/High/Critical), Team, Status label |
| **Text** | Free-form text — notes, URLs, reference numbers |
| **Date** | Calendar date — due date, review date, target release |

## Adding attributes to a board

Open the board and tap **Attributes** in the FAB menu (bottom right). Tap **+ Add attribute**, choose a type, give it a name, and (for Select) add the options.

Each attribute has a color used for the filter chips and attribute tags on cards.

## Setting attribute values on an issue

Open the issue detail and tap the edit icon. Attribute fields appear below the text content. For Select attributes, a dropdown shows the available options. For Text, a free-form input. For Date, a date picker.

Values are shown as colored tags in the card footer on the board.

## Filtering by attribute

When a board has Select attributes, filter chips appear in the filter bar below the navigation tabs. Tap a chip to filter to only issues with that value. Multiple filters combine — only issues matching all selected values are shown.

Tap **✕ Clear** to remove all active filters.

## Filtering in search

The global search also respects attribute values. When searching across boards you can combine text search with attribute filters.

## Common attribute setups

**Priority:** Select — Low, Medium, High, Critical (red color)

**Team:** Select — Frontend, Backend, Design, QA

**Due date:** Date

**Sprint:** Select — Sprint 1, Sprint 2, Sprint 3

**Environment:** Select — Dev, Staging, Production

## Attributes vs statuses

Statuses (columns) represent where in the workflow an issue is. Attributes represent additional facts about the issue that don't change its position in the workflow. An issue can be "In Progress" (status) and "High priority, Backend team, Due Jan 15" (attributes) at the same time.

## Deleting an attribute

In the Attributes management modal, tap the delete icon next to an attribute. The attribute and all its values are removed from all issues on the board. This cannot be undone.

## Related pages

- [Epics — boards and columns](/blog/documentation/laraue-boards/concepts/epics)
- [Search](/blog/documentation/laraue-boards/features/search)
- [Issues](/blog/documentation/laraue-boards/concepts/issues)
