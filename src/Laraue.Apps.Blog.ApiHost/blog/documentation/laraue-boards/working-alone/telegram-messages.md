---
title: Capturing Telegram messages — text, images, and video
description: How to capture any Telegram message as an issue in Laraue Boards. Supports text messages, photos, videos, and mixed media. Forward to the bot or capture directly from chats.
keywords: [forward telegram message task, save telegram message, telegram message to kanban, capture telegram photo task, telegram bot forward task]
type: documentation
project: boards
order: 3
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
The core idea behind Laraue Boards is that work is already happening in Telegram. A client request, a bug report, a photo of something that needs fixing — these are all tasks, they just haven't been structured yet. Laraue Boards captures them directly.

## Forwarding a message to the bot

The simplest way to capture a message:

1. Long-press any message in any Telegram chat
2. Tap **Forward**
3. Choose **@laraue_boards_bot** as the recipient
4. The message appears in your Backlog immediately

This works for text messages, photos, videos, voice messages, documents, and mixed media.

## What gets captured

When you forward a message, Laraue Boards saves:

- The **text content** of the message (up to 4096 characters)
- **Photos** — stored and shown as thumbnails on the issue card
- **Videos** — stored with progressive streaming support, so they play immediately without waiting for a full download
- **Sender name** and **source chat name** — shown on the card for context
- **Timestamp** — when the original message was sent

What is not captured: reactions, replies, forwarded-from attribution chains, and voice message transcriptions.

## Media in issue cards

If an issue has attached photos or videos, a horizontal strip of thumbnails appears on the card — up to four visible, with a `+N` overlay if there are more. The strip is horizontally scrollable.

Tapping any thumbnail opens the **media viewer**: a full-screen view with navigation between all attachments. Videos play with native controls. The viewer shows a `2 / 5` counter when there are multiple files.

## Media in the issue detail

The issue detail view shows a 3-column grid of all media files above the text content. Tapping any item opens the viewer at that position.

## Text-only issues

A message does not need text to become an issue. A photo with no caption becomes a media-only issue — the card shows the thumbnail strip without any text below the header.

## Manually created issues

Issues created by tapping **+ Add card** on a board column are not linked to any Telegram message. They have the same fields (content, status, attributes) but no sender, source chat, or media from Telegram.

## Supported media types

| Type                     | Supported                    |
|--------------------------|------------------------------|
| Photos (JPEG, PNG, WebP) | ✓                            |
| Videos (MP4, MOV)        | ✓ with progressive streaming |
| GIFs                     | ✓ treated as video           |
| Voice messages           | -                            |
| Documents / files        | -                            |
| Stickers                 | -                            |

## Related pages

- [Issues — what they are](/blog/documentation/laraue-boards/concepts/issues)
- [Media attachments](/blog/documentation/laraue-boards/features/media)
- [The Backlog](/blog/documentation/laraue-boards/working-alone/backlog)
