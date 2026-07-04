---
title: Why users kept choosing Telegram Saved Messages over our bot — and what we changed
description: Part 10 of building a Telegram task tracker solo. The story of a product mistake — we added epic and status selection to the bot, but real users kept using Saved Messages — and how we fixed it.
type: article
createdAt: 2026-06-25 13:00
updatedAt: 2026-07-04 13:00
projects: [boards]
tags: [product, ux, telegram-bot, saved-messages, devlog]
previousLink: issue-board-telegram-mini-app
nextLink: telegram-bot-file-storage-stream
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 10.
> In the [previous article](issue-board-telegram-mini-app) we implemented the issue domain and showed the app to its first users. This article is about the product mistake their feedback surfaced — and why we stopped trying to make the bot a full tool for managing boards.

Most of this series is about building. This article is about a feature we finished and then deleted. Real users found it inconvenient. The mistake let us define the principles of which functionality we develop in the bot and which in the web interface.

## The implemented feature: picking an epic and status for a task right in the chat

By this point the backend could work with epics, issues and statuses, and the web app showed the board and let the user work with it. Before presenting the result on social media, we decided to add an improvement to the bot chat, guided by simple logic: why force a person to open the web app to pick an epic and status for a task, if it can be done right in the chat, at the moment of saving?

It worked like this: the user sends a message to the bot, and it is saved. The bot then offers to pick an epic for the new record, and after that a status as well. Two taps, and the task is not just saved into the backlog for later — it sits in the right place immediately.

It sounded perfectly logical: more ways to organise issues without leaving Telegram. We spent quite some time on the implementation, and it was time to show the feature to users.

## The first feedback

We told about the bot for working with saved messages in our Telegram channel and on Threads and got some number of users. Mostly, people came in, had a look, and left forever. A few kept coming back. Besides that, the app was shown to close people — relatives and friends. Then the observations began.

The first signal: a family member did not use the bot when they wanted to write something down quickly — while continuing to use it in everyday life. The reason turned out to be that Saved Messages in those situations was used for saving thoughts there was no time to deal with right now. Saving through the bot, meanwhile, started the questions: which epic and which status does this belong to. Unconsciously, the user preferred Saved Messages, to avoid questions there is no time for.

The second signal was asking one of the bot's daily users about his usage scenarios. He answered that he uses the chat with the bot only for saving messages, and does everything else in the web interface.

Besides that, we discovered an unpleasant moment with forwarding a group of messages to the bot. In that scenario the chat with it became maximally confusing. Imagine: you forward 5 messages from some chat, and in response you get 5 questions from the bot in a row about which epic each one belongs to.

## Conclusions

There are two conclusions here, and the first is about us, not about the users.

**The first conclusion — developers have a professional deformation when working with bots.** Managing things through commands and inline buttons seems simple and concise to us. A user can find it off-putting: "Why is it asking so many questions? What happens if I don't answer them?" So we decided that the chat bot has to be maximally simple, not super functional.

**The second conclusion is about the product.** Laraue Boards was conceived as improved Saved Messages. As a place where thoughts become a card on a board without requiring extra actions from the user. And in the end we overcomplicated everything, trying to bet on functionality. The main virtue of Saved Messages is simplicity. So we have to stay simple enough to be their possible alternative.

## Rolling the feature back

The fix: delete the feature and make the bot maximally simple. A sent message = a 👍 reaction from the bot. That is the entire saving flow. If the user needs issue management — welcome to the app.

A clean chat turned out to have a second plus. Every interaction is an ordinary message. Which means Telegram's built-in search works over it: finding last week's note is possible right in the chat, without opening the Mini App — the same way as in the Saved Messages we compete with.

## An idea for the future: a Custom Reply Keyboard for the last message

Even though the main lesson was that the bot simply has to be user-friendly, we still understand that it needs functionality too. Otherwise the user has no incentive to move away from Saved Messages at all. We also still allow that a user may run into situations where they want to set an epic or a status without opening the app.

The idea for the future is to combine simplicity and functionality with a **Custom Reply Keyboard**: the message is saved without questions, as before. But the user is shown a set of buttons that allow changing the epic of the last message. Such a keyboard is always attached to the last message — so the chat stays clean.

## The rest of the feedback

Social media users voiced their opinions quite actively, and we discovered a few more problems.

The most frequent remark was distrust of the product. People wrote that they keep private data in Saved Messages that they are not ready to entrust to an unknown product. Among the suggestions: make the project open source, or allow a self-hosted run, so the data never leaves their own server. We promised to think about it.

There were other points too. Someone did not like the dark theme, someone absolutely could not understand what this product even is, and someone broke the bot by trying to save an image.

The collected feedback effectively gave us a list, from real users, of the direction they see our app developing in.

## What comes next

The next article starts with solving the image-saving problem one of the users ran into. But before improving anything, we first have to settle on the file storage infrastructure.