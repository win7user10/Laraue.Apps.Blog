---
title: Capturing Telegram messages — text, images, and video
description: How to capture any Telegram message as an issue in Laraue Boards. Supports text messages, photos, videos, and mixed media. Forward to the bot or capture directly from chats.
keywords: [forward telegram message task, save telegram message, telegram message to kanban, capture telegram photo task, telegram bot forward task]
type: documentation
project: boards
order: 3
createdAt: 2026-04-22
updatedAt: 2026-08-06
---
The core idea behind Laraue Boards is that work is already happening in Telegram. A client request, a bug report, a photo of something that needs fixing — these are all issues, they just haven't been structured yet. Laraue Boards captures them directly.

## Forwarding a message to the bot

The simplest way to capture a message:

1. Long-press any message in any Telegram chat
2. Tap **Forward**
3. Choose **@msgboard_bot** as the recipient
4. The message becomes an issue on your default board immediately

![A forwarded message in Telegram with the bot's reaction confirming it was saved](https://laraue.com/static/images/blog/docs/laraue-boards/message-board-bot-processed-message.jpg)

This works for text messages, photos, videos, and albums.

## What gets captured

When you forward a message, Laraue Boards saves:

- The **text content** of the message (up to 4096 characters)
- **Photos** and **videos** — shown as thumbnails on the issue card, fetched from Telegram in original quality when you open them
- **Sender name** — shown on the card for context
- **Timestamp** — when the original message was sent

What is not captured: reactions, replies, forwarded-from attribution chains, and voice message transcriptions.

## Attachments on the issue

There are no thumbnails on the board card — media only shows up once you open the issue. The detail page has an **Attachments** section below the content, showing every photo and video as a small thumbnail. Tap one to open the full image.

![The Attachments section on an issue's detail page](https://laraue.com/static/images/blog/docs/laraue-boards/issue-attachments.jpg)

From here you can also add more images yourself — **Choose other images**, or paste one directly with **Ctrl+V** — and remove any attachment with the **×** on its thumbnail.

## Text-only issues

A message does not need text to become an issue. A photo with no caption becomes a media-only issue — the card shows the thumbnail strip without any text below the header.

## Manually created issues

Issues created by tapping **+ Add issue** are not linked to any Telegram message. They have the same fields (content, status, attributes) but no sender or media from Telegram.

## Related pages

- [Issues — what they are](/blog/documentation/laraue-boards/concepts/issues)
- [The Backlog](/blog/documentation/laraue-boards/working-alone/backlog)