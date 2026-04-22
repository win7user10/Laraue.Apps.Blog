---
title: Organizations — collaborate with team
description: Laraue Boards organizations let multiple users share boards and spaces. Learn how to create an organization, invite members, and switch between personal and team workspaces.
keywords: [team project management telegram, shared kanban telegram, invite team telegram board, small team task tracker, organization workspace kanban]
type: documentation
project: boards
order: 4
createdAt: 2026-04-22
updatedAt: 2026-04-22
---

An organization is a shared workspace in Laraue Boards. When you work inside an organization, boards and spaces are visible to all members of that organization according to their permission level.

Personal boards remain completely separate — creating an organization does not share your personal work.

## Creating an organization

After logging in, you land on the workspace selection screen. Tap **New organization** and fill in:

- **Name** — the display name, e.g. "Laraue Dev" or "Acme Team"
- **Key** — a short identifier, e.g. `LRD`, shown in the workspace switcher
- **Color** — used in the breadcrumb and workspace switcher

You are automatically assigned as the **Owner** of the new organization.

## Inviting members

Open the organization management panel from the user avatar menu (top right) and select **My organization**.

On the **Members** tab, an **Invite link** is shown. Copy the link and send it to your teammate in Telegram. When they open the link and accept, they join the organization as a **Member**.

The invite link can be regenerated at any time to revoke all pending invitations without removing existing members.

## Member roles

| Role | What they can do |
|------|-----------------|
| **Owner** | Everything — delete org, manage all members and permissions |
| **Admin** | Create/edit/delete boards, manage members but not owner settings |
| **Member** | View and edit issues according to their permission settings |

Roles can be changed by the Owner or an Admin from the Members tab.

## Revoking access

On the Members tab, tap the revoke button next to a member's name. Their access is removed immediately. The Owner cannot be removed — ownership must be transferred first.

## Switching between workspaces

Tap the organization name in the breadcrumb at the top of the screen. A switcher appears showing:

- **Personal** — your private workspace
- Each organization you belong to
- **Manage organizations** — returns to the workspace selection screen

Switching workspace context changes which boards and spaces are shown in the navigation.

## Editing or deleting an organization

Return to the workspace selection screen via **Manage organizations** in the switcher. Tap the edit or delete button next to an organization you own.

Deleting an organization permanently removes all boards, spaces, and issues belonging to it. This cannot be undone.

## Related pages

- [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions)
- [Member management](/blog/documentation/laraue-boards/working-in-a-team/member-management)
- [Spaces — grouping boards](/blog/documentation/laraue-boards/concepts/spaces)
