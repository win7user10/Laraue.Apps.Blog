---
title: Permissions management — who can see and edit what
description: How Laraue Boards permissions work across organization, space, and epic levels. Configure read, create, update, delete, and manage access for each team member.
keywords: [project management permissions, kanban access control, team permissions task tracker, role based access telegram board, granular permissions project management]
type: documentation
project: boards
order: 2
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
Laraue Boards uses a hierarchical permission model. Permissions can be granted at three levels: the whole organization, a specific space, or a specific epic (board). A higher-level grant automatically covers all levels below it.

## Permission types

| Permission | What it allows |
|------------|----------------|
| **Read** | View issues, boards, and spaces |
| **Create** | Create new issues and boards |
| **Update** | Edit existing issues and board settings |
| **Delete** | Delete issues and boards |
| **Manage** | All of the above plus managing members and permissions |

## How hierarchy works

If a user has **Read** permission at the Organization level, they can read everything inside that organization — all spaces and all boards — without needing it granted at each level individually.

If they only have **Read** at the Space level for "Mobile App", they can see all boards within that space but cannot see boards in other spaces.

If they only have **Read** at the Epic level for "Sprint 1", they can see only that board.

**Higher levels override lower levels.** If an organization-level permission is granted, the space and epic checkboxes are automatically checked and disabled — they are inherited and cannot be individually removed.

## Configuring permissions for a member

Open **My organization** from the user avatar menu. On the **Members** tab, tap the permissions icon (bar chart icon) next to a member's name.

The permissions tab shows a table:

- Rows: Organization → Spaces (all) → Individual spaces → Epics (all) → Individual epics
- Columns: Manage, Create, Read, Update, Delete
- Disabled checkboxes indicate inherited permissions from a parent level

Check or uncheck each cell to grant or revoke. Tap **Save** when done.

## Practical examples

**Contractor who should see only one space:**
- No organization-level permissions
- Read permission on the specific space only

**Developer who can work on issues but not delete anything:**
- Organization-level Read
- Organization-level Create and Update
- No Delete at any level

**Full team member:**
- Organization-level Read, Create, Update
- Manage on spaces they own

**Admin:**
- Organization-level Manage (covers everything)

## Default permissions for new members

New members who join via invite link receive **Read** permission at the organization level by default. They can see all boards but cannot create, edit, or delete anything until you explicitly grant those permissions.

You can change the default before sharing the invite link by configuring a template member, or adjust permissions individually after they join.

## Related pages

- [Organizations](/blog/documentation/laraue-boards/concepts/organizations)
- [Member management](/blog/documentation/laraue-boards/working-in-a-team/member-management)
- [Creating an organization](/blog/documentation/laraue-boards/working-in-a-team/creating-organization)
