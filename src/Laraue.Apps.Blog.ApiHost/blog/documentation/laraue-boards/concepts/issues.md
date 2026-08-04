---
title: Issues — the base entities
description: An issue in Laraue Boards is a single task. Learn how issues are created from Telegram messages, how issue keys work, and how to manage issue details.
keywords: [telegram message to task, issue tracker telegram, task from telegram message, kanban issue telegram, jira issue alternative]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-08-04
---
An issue is the core unit of work in Laraue Boards. Every task, request, bug report, or action item is an issue. What makes Laraue Boards different is where issues come from: most of them start as Telegram messages.

## What an issue contains

Every issue has:

- **Content** — the text of the message, or manually entered text
- **Sender** — who sent the original Telegram message
- **Status** — which column on the board it currently sits in
- **Issue key** — a unique identifier like `WRK-42`
- **Attributes** — any custom fields you have defined for the board
- **Media** — attached photos, videos, or albums from the original message

## Issue keys

Every issue receives a key based on its space, not its board: three letters from the space name, followed by a number that is sequential within that space — for example `WRK-1`, `WRK-2`, `MOB-14`. Moving an issue to a different board inside the same space does not change its key. Only moving it to a different space would.

Issue keys let you reference tasks in conversation. Write "see WRK-42" in a Telegram message and everyone on the team knows exactly which task you mean, without needing to share a link.

Every issue also has a direct link, built from its organization and its key:

```
https://boards.laraue.com/organizations/{OrgKey}/issues/{IssueKey}
```

Share the link when you want to take someone straight to the issue, rather than just naming it.

## Creating issues

**From a Telegram message** — forward any message to the Laraue Boards bot. It is saved automatically, with no extra steps, and lands on the default space in your Backlog. The bot reacts with 👍 to confirm.

Right now, every issue created this way lands in your **personal organization**, regardless of which chat the message came from. Mapping specific group chats to specific organizations — so a message from a client's chat lands directly in that client's organization — is planned for a future version.

**Manually** — tap the **+ Add issue** button in the top right corner of the interface. Enter the content directly.

## Moving an issue to a different board

New issues from the bot land on the default board so saving stays effortless — you never have to pick a board before sending a message. If you want an issue somewhere else, open the **Backlog**, find the issue in the list, and change its board directly from the issue's detail view.

Moving an issue between boards **within the same space** keeps its issue key. Moving it to a **different space** gives it a new key, since keys are numbered per space.

## Moving an issue between statuses

Drag the card from one column to another. The issue key stays the same — moving between columns does not change the key, only the status.

## Media in issues

If the original Telegram message contained photos, videos, or an album, they are preserved in the issue as a single card with all attachments. Files are not stored separately — they are fetched from Telegram when you open them, in their original quality. The card on the board shows a strip of up to four thumbnails. Tapping a thumbnail opens the full-screen media viewer with navigation between all attachments.

## Editing an issue

If you edit the original message in Telegram, the issue updates with it. The bot's reaction changes to ❤ to confirm the edit was picked up.

## Related pages

- [Epics — organizing issues into boards](/blog/documentation/laraue-boards/concepts/epics)
- [Spaces](/blog/documentation/laraue-boards/concepts/spaces)
- [Custom attributes](/blog/documentation/laraue-boards/features/attributes)
- [Issue keys reference](/blog/documentation/laraue-boards/features/card-keys)
- [Media attachments](/blog/documentation/laraue-boards/features/media)