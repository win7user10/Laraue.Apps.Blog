---
title: Using Laraue Boards inside Telegram
description: Laraue Boards runs as a Telegram Mini App — open it directly from the bot without leaving Telegram. No separate login, no context switching, full functionality on mobile.
keywords: [telegram mini app project management, telegram web app kanban, open telegram mini app, telegram bot task manager, mini app boards telegram]
type: documentation
project: boards
order: 2
createdAt: 2026-04-22
updatedAt: 2026-08-06
---
Laraue Boards is built as a **Telegram Mini App** — a web application that runs inside the Telegram client. This means you can use it without leaving Telegram, without a separate account, and without switching between apps.

## Opening the Mini App

Search for **@msgboard_bot** in Telegram and tap **Start**. The Mini App opens in the bottom sheet inside Telegram. On mobile this fills most of the screen; on desktop it opens in a side panel.

![The Mini App launch button next to the bot's chat](https://laraue.com/static/images/blog/docs/laraue-boards/message-board-bot-launch-mini-app-button.jpg)

You can also pin the bot to your chat list for quick access.

## How login works in the Mini App

When you open the Mini App inside Telegram, you are already logged in. Telegram passes your identity to the app automatically and securely — there is no login button, no widget, no redirect. You go directly to the organization selection screen, the same one described in [Authorization](/blog/documentation/laraue-boards/getting-started/authorization).

![The organization selection screen, listing Personal and a team organization, with a Create organization button](https://laraue.com/static/images/blog/docs/laraue-boards/login-organization.jpg)

This uses Telegram's `initData` mechanism: a signed payload containing your user information that the server verifies using a cryptographic hash. Your credentials cannot be spoofed.

## What works in the Mini App vs the browser

Both the Mini App and the web browser version have identical features. The differences are:

| | Telegram Mini App | Web browser |
|---|---|---|
| Login | Automatic, no steps | Telegram widget + approval |
| Capturing messages | Forward to bot from any chat | Same |
| Screen size | Mobile-first, bottom sheet | Full desktop or mobile |

## Safe area and notch support

The Mini App is aware of the device's safe area — on iPhones with a notch or Dynamic Island, and Android devices with punch-hole cameras, the UI adjusts automatically so no content is hidden behind hardware cutouts.

## Sending issues from chats

Inside Telegram, if you are reading a message and want to turn it into an issue, forward it to **@msgboard_bot** without leaving your current conversation — swipe the message and use the Forward button. The issue appears in your Backlog immediately.

## Using Laraue Boards without Telegram

The web app at [boards.laraue.com](https://boards.laraue.com) works independently of the Telegram client. You log in once via the Telegram widget and then use it like any other web app. This is useful when:

- You are working from a computer where Telegram is not installed
- Your team has some members who prefer a browser-based workflow
- You want to use it on a large screen with more board columns visible

## Related pages

- [Authorization](/blog/documentation/laraue-boards/getting-started/authorization)
- [Quick start guide](/blog/documentation/laraue-boards/getting-started/quick-start)