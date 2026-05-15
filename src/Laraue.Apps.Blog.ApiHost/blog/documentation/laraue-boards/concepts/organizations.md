---
title: Organizations — collaborate with your team
description: Laraue Boards organizations let multiple users share boards and spaces. Learn how to create an organization, invite members, and switch between personal and team workspaces.
keywords: [team project management telegram, shared kanban telegram, invite team telegram board, small team task tracker, organization workspace kanban]
type: documentation
project: boards
order: 4
createdAt: 2026-04-22
updatedAt: 2026-05-15
---

An organization is a shared workspace in Laraue Boards. When you work inside an organization, boards and spaces are
visible to members of that organization according to their permission level.

Personal boards remain completely separate — creating an organization does not share your personal work.

## Creating an organization

After logging in, you land on the workspace selection screen. Tap **New organization** and fill in:

- **Name** — the display name, e.g. "Laraue Dev" or "Acme Team"
- **Key** — a short identifier, e.g. `LRD`, shown in the workspace switcher
- **Color** — used in the breadcrumb and workspace switcher

You are automatically assigned as the **Owner** of the new organization.

## Inviting members

Open the organization management panel from the user avatar menu (top right) and select **Manage organization**.

On the **Members** tab, an **Invite link** is shown. Copy the link and send it to your teammate in Telegram.
When they open the link and accept, they join the organization as a **Member**.

The invite link can be regenerated at any time to revoke all pending invitations without removing existing members.

## Permissions
When the user become an organization member, he doesn't have any permission as default. Click **Edit Permissions** on the 
Member row to set up access.  
The **Edit Permissions** window has three tabs that allow to make flexible organization access:

| Tab                | What it do                                                                      |
|--------------------|---------------------------------------------------------------------------------|
| **Organization**   | Allow to make global permissions setup. E.g. allow view of all issues           |
| **Direct**         | Create/edit/delete specific spaces/boards. E.g. allow reading backlog epic only |
| **Administrative** | Setup permissions to manage organization                                        |


## Revoking access

On the Members tab, tap the revoke button next to a member's name. Their access is removed immediately.
The Owner cannot be removed — ownership must be transferred first.

## Switching between workspaces

Tap the organization name in the breadcrumb at the top of the screen. A switcher appears showing:

- **No organization** — your private workspace
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
