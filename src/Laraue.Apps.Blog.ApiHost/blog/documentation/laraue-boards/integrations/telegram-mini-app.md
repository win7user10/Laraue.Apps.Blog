---
title: Using Laraue Boards inside Telegram
description: Laraue Boards runs as a Telegram Mini App — open it directly from the bot without leaving Telegram. No separate login, no context switching, full functionality on mobile.
keywords: [telegram mini app project management, telegram web app kanban, open telegram mini app, telegram bot task manager, mini app boards telegram]
type: documentation
project: boards
order: 2
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
Laraue Boards is built as a **Telegram Mini App** — a web application that runs inside the Telegram client. This means you can use it without leaving Telegram, without a separate account, and without switching between apps.

## Opening the Mini App

Search for **@laraue_boards_bot** in Telegram and tap **Start**. The Mini App opens in the bottom sheet inside Telegram. On mobile this fills most of the screen; on desktop it opens in a side panel.

You can also pin the bot to your chat list for quick access.

## How login works in the Mini App

When you open the Mini App inside Telegram, you are already logged in. Telegram passes your identity to the app automatically and securely — there is no login button, no widget, no redirect. You go directly to the workspace selection screen.

This uses Telegram's `initData` mechanism: a signed payload containing your user information that the server verifies using a cryptographic hash. Your credentials cannot be spoofed.

## What works in the Mini App vs the browser

Both the Mini App and the web browser version have identical features. The differences are:

| | Telegram Mini App | Web browser |
|---|---|---|
| Login | Automatic, no steps | Telegram widget + approval |
| Capturing messages | Forward to bot from any chat | Same |
| Notifications | Via Telegram directly | Browser notifications (if enabled) |
| Offline | Not supported | Not supported |
| Screen size | Mobile-first, bottom sheet | Full desktop or mobile |

## Safe area and notch support

The Mini App is aware of the device's safe area — on iPhones with a notch or Dynamic Island, and Android devices with punch-hole cameras, the UI adjusts automatically so no content is hidden behind hardware cutouts.

## Sending issues from chats

Inside Telegram, if you are reading a message and want to turn it into a task, forward it to @laraue_boards_bot without leaving your current conversation — swipe the message and use the Forward button. The task appears in your Backlog immediately.

## Using Laraue Boards without Telegram

The web app at [laraue.com/msgboard](https://laraue.com/msgboard) works independently of the Telegram client. You log in once via the Telegram widget and then use it like any other web app. This is useful when:

- You are working from a computer where Telegram is not installed
- Your team has some members who prefer a browser-based workflow
- You want to use it on a large screen with more board columns visible

## Related pages

- [Authorization](/blog/documentation/laraue-boards/getting-started/authorization)
- [Capturing Telegram messages](/blog/documentation/laraue-boards/working-alone/telegram-messages)
- [Quick start guide](/blog/documentation/laraue-boards/getting-started/quick-start)
