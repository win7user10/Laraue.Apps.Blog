---
title: Logging in with Telegram
description: How to log into Laraue Boards using your Telegram account, both inside the Telegram Mini App and via the web browser login widget.
keywords: [telegram login, telegram widget login, telegram mini app authentication, login without password telegram]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-04-27
---

Laraue Boards uses your existing Telegram account as your identity. There is no separate registration, no password to remember, and no email to verify.

## Two ways to open Laraue Boards

### As a Telegram Mini App

Search for **@laraue_boards_bot** in Telegram and tap **Start**. The Mini App opens inside Telegram — you are instantly logged in using your Telegram identity. No widget, no redirect, no extra steps.

This is the recommended way to use Laraue Boards on mobile.

### In the browser

Open [boards.laraue.com](https://boards.laraue.com) in any browser. You will see the login screen with a **Log in with Telegram** button. Clicking it opens a Telegram authorization popup — approve it and you are redirected back to the app.

The browser version requires that your Telegram account is accessible on the same device, or via the Telegram web client.

## What data is used

When you log in, Laraue Boards receives your Telegram user ID, first name, last name (if set), username (if set), and profile photo URL. No messages are read — only your identity is used for authentication.

The login is verified server-side using a cryptographic hash signed with the bot token, so the data cannot be forged.

## Switching accounts

Laraue Boards does not support multiple simultaneous Telegram accounts. To switch accounts, log out and log in again with a different Telegram account.

## After login: choosing a workspace

After logging in for the first time you will see the **workspace selection screen**. You can choose:

- **Personal** — a private workspace only you can see, available to everyone immediately
- **An organization** — a shared workspace you belong to (if you have been invited to one)

You can switch workspaces at any time from the breadcrumb at the top of the screen.

## Related pages

- [Using Laraue Boards inside Telegram](/blog/documentation/laraue-boards/integrations/telegram-mini-app)
- [Creating an organization](/blog/documentation/laraue-boards/working-in-a-team/creating-organization)
