---
title: Issue keys — reference tasks like WRK-42
description: Every issue in Laraue Boards gets a unique key like WRK-42. Learn how keys are generated, how to reference them in Telegram, and how to search by key.
keywords: [issue key task tracker, jira issue key alternative, unique task id kanban, reference task telegram, WRK-42 issue identifier]
type: documentation
project: boards
order: 4
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
Every issue assigned to a board receives a unique key — a short alphanumeric identifier like `WRK-42`, `MOB-7`, or `BUG-103`. Keys let you reference specific tasks in conversation without needing to share a link.

## How keys are generated

The key has two parts: the board prefix and a sequential number.

**Board prefix** is derived from the board name — by default the first three uppercase letters of the name, with non-alphabetic characters removed. "Work" becomes `WRK`, "Sprint 1" becomes `SPR`, "Bug Fixes" becomes `BUG`. You can set a custom prefix when creating or editing a board.

**Sequential number** starts at 1 and increments with each issue assigned to that board. Numbers are never reused — if issue `WRK-5` is deleted, the next issue becomes `WRK-6`, not `WRK-5`.

## When a key is assigned

An issue receives a key the moment it is assigned to a board. Issues in the Backlog that have not been assigned to any board use a temporary `MSG-N` key.

When an issue is moved from one board to another, it receives a new key from the destination board. Moving between columns within the same board does not change the key.

## Using keys in Telegram

Write the key in any Telegram message — `see WRK-42` or `fixed in MOB-15` — and anyone with access to the board immediately knows which task you mean. You do not need to share a URL.

Keys are searchable in Laraue Boards. Type `WRK-42` in any search field to find that exact issue.

## Where keys appear

- **On the board card** — shown in small monospace text next to the sender name
- **In the issue detail** — displayed as a prominent pill badge in the header
- **In search results** — shown alongside the issue content

## Keys and Jira

If your team is migrating from Jira, the key format will feel familiar. The main differences:

- Laraue Boards keys are per-board, not per-project
- There is no way to manually set a key number
- Keys are not linkable directly — use search to navigate to a key

## Related pages

- [Issues — what they are](/blog/documentation/laraue-boards/concepts/issues)
- [Epics — boards and their prefixes](/blog/documentation/laraue-boards/concepts/epics)
- [Search](/blog/documentation/laraue-boards/features/search)
