---
title: Auto vs. manual save mode for linked chats
description: Once a Telegram chat is linked to Boards, choose whether every message becomes an issue automatically, or only the ones you save yourself with /save. Includes /info for looking up a card without saving.
keywords: [telegram bot save mode, telegram bot auto save, telegram save command, telegram info command, telegram bot manual save]
type: documentation
project: boards
order: 4
createdAt: 2026-08-19
updatedAt: 2026-08-20
---
**Save mode** is the last step of [linking a chat](/blog/documentation/laraue-boards/integrations/telegram-linking), and it decides *when* a message in that chat turns into an issue: automatically, or only when someone asks for it.

## Every message (auto mode)

Every message sent in the chat becomes an issue immediately. If the sender edits their message afterward, the issue updates to match — auto mode keeps the issue in sync with the live message.

This is a good choice for a **solo or personal chat** — for example, your own "notes to self" chat linked to a specific project, where any message should become a card.

Because saving happens silently (a reaction confirms it, not a reply), reply to a message with `/info` (below) if you want to grab the card's link.

## Only via /save (manual mode)

Nothing is saved automatically. Messages are tracked so they *can* be saved, but no issue exists until someone acts on one.

To save a message, **reply to it** with `/save`, optionally adding a note:

```
/save follow up next sprint
```

The note becomes the top of the issue's content, with the original message's text below a `---` divider if both are present.

![Replying to a message with /save and a note in a group chat](https://laraue.com/static/images/blog/docs/laraue-boards/telegram-save-command.jpg)

This is the right choice for a **busy group chat** where only some messages should become cards.

Replying to any message in a photo/video **album** saves the whole album as one issue — content and caption come from the album's first message, and every attachment is attached to the issue.

Running `/save` again on a message that's already saved triggers a re-sync and returns the same card's link. In manual mode, this is the correct way to update the card on the board if the message in the chat was updated.

## /info — look up without saving

`/info` works the same way in both modes: reply to a tracked message to see the same card preview. It's most useful in auto mode, where the save happens silently and you need the link.

## Permissions

Every save checks whether you have create-issue permission in the destination epic. Linking a chat doesn't hand everyone in it permission to create cards there. See [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions) for how to grant it.

## Related pages

- [Linking a Telegram chat to Boards](/blog/documentation/laraue-boards/integrations/telegram-linking)
- [Capturing Telegram messages](/blog/documentation/laraue-boards/working-alone/telegram-messages) — forwarding messages without linking a chat
- [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions)
