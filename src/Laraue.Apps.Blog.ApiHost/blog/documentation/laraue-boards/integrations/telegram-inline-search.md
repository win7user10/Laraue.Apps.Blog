---
title: Searching issues from any Telegram chat
description: Type @msgboard_bot followed by a query in any Telegram chat to search your issues with Telegram's inline mode — filter by assignee, space, or date, or search by free text, without opening the app.
keywords: [telegram inline search issues, search kanban from telegram, telegram bot find task, telegram inline query board]
type: documentation
project: boards
order: 5
createdAt: 2026-08-19
updatedAt: 2026-08-20
---
You don't need a linked chat, or even a chat with the bot at all, to find an issue. Typing `@msgboard_bot` followed by a query in **any** Telegram chat searches your issues right there, using Telegram's inline query feature.

## Running a search

Start typing `@msgboard_bot ` in the message box of any chat, group, or channel where you can type, followed by what you're looking for. Telegram shows matching results above the keyboard as you type — tap one to send it into the chat.

![Typing an inline query for the bot and seeing matching issues appear above the keyboard](https://laraue.com/static/images/blog/docs/laraue-boards/inline-search.jpg)

Results are scoped to every space you have read access to, across every organization you belong to — never anyone else's data.

## What you can search by

The query can mix structured filters with free text:

- **Filter tokens**, written as `key:value` — filter by assignee, organization, space, or how recently an issue was updated (for example, updated in the last few days versus older than that)
- **An exact issue key** — typing something like `UNC-24` jumps straight to that issue
- **Free text** — matched against issue content, with the matching part highlighted in the result so you can see why it matched, not just that it did

## What a result looks like

Every search result renders as the same issue preview card used elsewhere in the bot (in `/save` and `/info` replies too):

- The header shows the **issue key and organization**, for example `UNC-24 · Laraue Corp`
- The body is a snippet of the issue's content centered on the match, or the start of the content for lookups that aren't text searches
- A footer with the **source chat, sender, and timestamp** appears only for issues that came from a Telegram message — and shows who actually sent the original message, captured at the time it arrived, independent of who later ran `/save` on it. Issues created in the web app, or missing that information, simply have no footer

Tap **🔗 Open issue** on a result to deep-link straight into the web app.

## Related pages

- [Linking a Telegram chat to Boards](/blog/documentation/laraue-boards/integrations/telegram-linking)
- [Auto vs. manual save mode](/blog/documentation/laraue-boards/integrations/telegram-save-modes)
- [Search — find issues on a board or in the Backlog](/blog/documentation/laraue-boards/features/search)
- [Issue keys](/blog/documentation/laraue-boards/features/card-keys)
