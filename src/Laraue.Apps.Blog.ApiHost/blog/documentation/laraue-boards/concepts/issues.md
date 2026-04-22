---
title: Issues — the base entities
description: An issue in Laraue Boards is a single task. Learn how issues are created from Telegram messages, how issue keys work, and how to manage issue details.
keywords: [telegram message to task, issue tracker telegram, task from telegram message, kanban issue telegram, jira issue alternative]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
An issue is the core unit of work in Laraue Boards. Every task, request, bug report, or action item is an issue. What makes Laraue Boards different is where issues come from: most of them start as Telegram messages.

## What an issue contains

Every issue has:

- **Content** — the text of the message, or manually entered text
- **Sender** — who sent the original Telegram message
- **Source chat** — which Telegram group or chat the message came from
- **Status** — which column on the board it currently sits in
- **Issue key** — a unique identifier like `WRK-42`
- **Attributes** — any custom fields you have defined for the board
- **Media** — attached photos or videos from the original message

## Issue keys

Every issue that gets assigned to a board receives a key based on the board's prefix and a sequential number — for example `WRK-1`, `WRK-2`, `MOB-14`. The prefix is derived from the board name (first three letters, uppercase) and can be customized when creating a board.

Issue keys let you reference tasks in conversation. Write "see WRK-42" in a Telegram message and everyone on the team knows exactly which task you mean, without needing to share a link.

Issues that are in the backlog but not yet assigned to any board receive a `MSG-N` key.

## Creating issues

**From a Telegram message** — forward any message to the Laraue Boards bot. It appears in your Backlog. Open it and tap **Assign to board** to place it on a board.

**Manually** — tap the **+** button at the bottom of any status column on a board. Enter the content directly.

## Assigning an issue to a board

When you assign an issue, a three-step picker opens:

1. **Space** — choose which space the board belongs to (skipped if you have no spaces)
2. **Board** — choose the board
3. **Status** — choose which column to place the issue in

Once assigned, the issue receives its board key and appears in the chosen column.

## Moving an issue between statuses

Drag the card from one column to another. The issue key stays the same — moving between columns does not change the key, only the status.

To move an issue back to the Backlog, open the issue detail and tap **↩ Move to Backlog**.

## Media in issues

If the original Telegram message contained photos or videos, they are preserved in the issue. The card on the board shows a strip of up to four thumbnails. Tapping a thumbnail opens the full-screen media viewer with navigation between all attachments.

## Related pages

- [Epics — organizing issues into boards](/blog/documentation/laraue-boards/concepts/epics)
- [Custom attributes](/blog/documentation/laraue-boards/features/attributes)
- [Issue keys reference](/blog/documentation/laraue-boards/features/card-keys)
- [Media attachments](/blog/documentation/laraue-boards/features/media)
