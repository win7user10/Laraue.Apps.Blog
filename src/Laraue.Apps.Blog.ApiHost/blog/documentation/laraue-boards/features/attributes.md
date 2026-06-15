---
title: Custom attributes — filter and label your issues
description: Add custom fields to your Laraue Boards issues. Use text or select attributes to label priority, team, sprint, and anything else your workflow needs — then filter the board by those values.
keywords: [custom fields kanban, issue attributes project management, priority field task tracker, custom labels kanban board, filter by attribute kanban, kanban board custom fields, task metadata project management]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-06-15
---

Attributes are custom fields attached to every issue on a board. They let you add structured metadata — priority, team, client name, sprint, or anything your workflow requires — and filter the board by those values.

Attributes are defined at the **organization level**, so the same attribute library is available across all boards in your organization.

## Attribute types

| Type       | Use for                                                                         | Status       |
|------------|---------------------------------------------------------------------------------|--------------|
| **Select** | Fixed list of options — Priority (Low/Medium/High/Critical), Team, Status label | Available    |
| **Text**   | Free-form text — notes, URLs, reference numbers                                 | Available    |
| **Date**   | Calendar date — due date, review date, target release                           | Coming soon  |

> **Note:** Date attributes are not yet implemented. Select and Text attributes are fully supported.

## Who can manage attributes

The **Manage Attributes** tab is visible in the board navigation to users with the **Manage Attributes** permission. By default, this permission is granted to the organization administrator. Other members see and use attributes but cannot create, edit, or delete them.

## Adding attributes

Attributes are created at the organization level and are then available on every board. Open any board and tap **Manage Attributes** in the navigation tabs. On the Manage Attributes page, tap **+ Add attribute**, choose a type (Select or Text), give it a name, and — for Select — add the available options.

Each attribute has a color used for the filter chips and attribute tags on cards.

## Setting attribute values on an issue

Open the issue detail and tap the edit icon. Attribute fields appear below the text content. For Select attributes, a dropdown shows the available options. For Text, a free-form input.

Values are shown as colored tags in the card footer on the board.

## Filtering by attribute

When a board has Select attributes, filter chips appear in the filter bar below the navigation tabs. Tap a chip to filter to only issues with that value. Multiple filters combine — only issues matching all selected values are shown.

Tap **✕ Clear** to remove all active filters.

## Filtering in search

The global search also respects attribute values. When searching across boards you can combine text search with attribute filters.

## Common attribute setups

**Priority:** Select — Low, Medium, High, Critical (red color)

**Team:** Select — Frontend, Backend, Design, QA

**Sprint:** Select — Sprint 1, Sprint 2, Sprint 3

**Environment:** Select — Dev, Staging, Production

**Notes:** Text — free-form context or links

## Attributes vs statuses

Statuses (columns) represent where in the workflow an issue is. Attributes represent additional facts about the issue that don't change its position in the workflow. An issue can be "In Progress" (status) and "High priority, Backend team" (attributes) at the same time.

## Deleting an attribute

On the Manage Attributes page, tap the delete icon next to an attribute. The attribute and all its values are removed from all issues on the board. This cannot be undone.

## Related pages

- [Epics — boards and columns](../concepts/epics)
- [Search](../features/search)
- [Issues](../concepts/issues)
