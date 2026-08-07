---
title: Custom attributes — filter and label your issues
description: Add custom fields to your Laraue Boards issues. Use text or list attributes to label priority, type, or anything else your workflow needs — then filter the board by those values.
keywords: [custom fields kanban, issue attributes project management, priority field task tracker, custom labels kanban board, filter by attribute kanban]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-08-07
---
Attributes are custom fields you can attach to issues — priority, type, or anything else your workflow needs. They're defined at the **organization level**, so the same attributes are available across every board in the organization.

![The Attributes settings page, listing existing attributes with their types](https://laraue.com/static/images/blog/docs/laraue-boards/attributes-list.jpg)

## Attribute types

| Type       | Use for                                                                        | Status      |
|------------|--------------------------------------------------------------------------------|-------------|
| **List**   | A fixed set of options — priority levels, issue type, anything from a dropdown | Available   |
| **Text**   | Free-form text — notes, links, reference numbers                               | Available   |
| **Date**   | A calendar date                                                                | Coming soon |
| **Number** | A numeric value                                                                | Coming soon |

## Managing attributes

Open **Settings → Attributes** in the sidebar. Tap **+ New attribute**, give it a name, pick a color, and choose the type.

![The Create attribute form, with Name, Color, and Type fields](https://laraue.com/static/images/blog/docs/laraue-boards/create-attribute.jpg)

For a List attribute, add its options — tap **+ Add option** for each one, and remove any with the trash icon next to it.

![The Edit attribute page for a List attribute, showing its options](https://laraue.com/static/images/blog/docs/laraue-boards/edit-attribute.jpg)

## Who can manage attributes

Managing attributes — creating, editing, or deleting them — requires the **Manage attributes** administrative permission. This isn't granted to anyone by default; a member needs it explicitly, the same as any other permission. Members without it can still see and use attributes on issues.

## Filtering by attribute

When a board has List attributes in use, filter chips appear in the filter bar. Tap a chip to filter to only issues with that value. Multiple filters combine — only issues matching all selected values are shown.

## Attributes vs statuses

Statuses (columns) represent where in the workflow an issue is. Attributes represent additional facts about the issue that don't change its position in the workflow. An issue can be "In Progress" (status) and "High priority" (attribute) at the same time.

## Deleting an attribute

On its edit page, tap **Delete attribute**. Since attributes are defined at the organization level, this removes it — and its values — from every issue across the organization, not just one board. This cannot be undone.

## Related pages

- [Epics — boards and columns](/blog/documentation/laraue-boards/concepts/epics)
- [Issues](/blog/documentation/laraue-boards/concepts/issues)