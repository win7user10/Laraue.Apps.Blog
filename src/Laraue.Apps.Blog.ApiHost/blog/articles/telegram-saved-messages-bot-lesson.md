---
title: Why users kept choosing Telegram Saved Messages over our bot — and what we cut
description: Part 10 of building a Telegram task tracker solo. The product mistake at the centre of the project — adding epic and status selection to the capture bot, watching real users keep reaching for Saved Messages instead, and cutting it back to one frictionless action.
type: article
createdAt: 2026-06-25 13:00
updatedAt: 2026-06-25 13:00
projects: [boards]
tags: [product, ux, telegram-bot, saved-messages, devlog]
previousLink: issue-board-telegram-mini-app
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 10.
> The [previous article](issue-board-telegram-mini-app) built the issues layer and promised this story: the product mistake that is the real reason management lives in the web app and not the bot.

Most of this series is about building things. This article is about tearing one out — a feature we spent real time on, finished, and then deleted, because the real people we put it in front of kept quietly reaching for the tool we were trying to replace. It is the most useful mistake on the project, and the lesson behind it shaped where every feature lives.

## The feature that seemed obviously good

By this point the backend understood epics, issues, and statuses, and the web app could show a board. So we added what felt like an obvious improvement: why make someone open the web app to organise a task when they could do it right there in the chat, the moment they captured it?

So we built exactly that. You would send a message to the bot — the capture the whole product is built around — and instead of just saving it, the bot would follow up. First it asked you to pick an epic for the new item. Then, once you chose, it asked you to pick a status. Two quick selection steps, right in the chat, and your task was not just captured but filed.

On paper this is a feature, not a bug. It adds capability; it lets you do more without leaving Telegram. We spent a good amount of time getting it working — the selection prompts, the state between them, handling the choices. It felt like progress.

## Putting it in front of real people

With the build finished, we did the thing that actually tests a product: we gave it to people.

We handed it to a family member — a casual user with no stake in the project — to store notes and organise them later. We announced it in a Telegram channel. And one friend became a genuine user — he had been organising all of his work through Telegram's Saved Messages already, so a task tracker that lived in Telegram was a natural fit for exactly how he worked.

Then we watched what people did, which is always more honest than what they say.

The first signal was quiet and easy to miss: the family member simply did not use the bot when she was in a hurry. Not a complaint, not a bug report — just non-use, in exactly the moment the app was supposed to be most useful. Thinking about why led straight to the problem. The whole point of capturing a thought is that you have no time and no attention to spare for it — that is *why* you are dumping it somewhere instead of dealing with it. And our bot, at precisely that moment, responded by asking two questions: which epic, which status. The friction landed exactly where friction is fatal. When there is no time, an interrogation is the last thing you want, so she did the rational thing and used something that did not interrogate her.

It was at its most absurd with batch forwarding, which is one of the most natural things to do with Saved Messages: you select five messages and send them all at once. With our bot, that did not produce five saved items — it produced five interrogations. Forward five messages, and the bot asks five times which board this one belongs to, then five times about the status. A single quick gesture turned into a ten-question form. Seeing that happen made the problem impossible to rationalise away: we had taken the single most frictionless action a user could perform and attached a queue of questions to it.

The second signal confirmed it. We asked the friend — an engaged, daily user, the best possible case for the feature — how he actually used the app. His answer was simple: the chat is for saving messages. That is it. The power user and the casual user, the two ends of the spectrum, were telling us the same thing from opposite directions. Neither was using the chat to manage anything. Both were using it to do exactly one thing: get a thought out of their head, fast.

The wider feedback said the same. When we posted the app on Threads, the most common reaction was recognition: yes, people stash things in Saved Messages all the time, and yes, finding something in there later is a real pain. That is the exact problem Boards exists to solve, so the validation was encouraging — people recognised the itch. But the same threads kept asking a pointed question: *why is this better than just using Saved Messages?* And every selection step we had added to the chat was an argument for the wrong answer. If the bot makes you do more work than Saved Messages to save the same note, the honest reply to "why is this better" becomes "for capture, it isn't." The feedback was not telling us to add more to the chat. It was telling us the chat had to win on the one thing Saved Messages already did effortlessly.

## The lesson we should not have needed

There are two lessons here, and the first is about us, not the users.

**Developers are deformed about chat.** To a developer, driving something through chat commands and inline buttons feels simple — it is text, it is direct, it is quick to build, and we are entirely comfortable with command-style interaction. That comfort is a professional deformation. For most people, a chat is a chat: a place to type a thing, not a control panel to operate through menus and prompts. What reads as "powerful and convenient" to the person building it reads as "why is this asking me so many questions" to the person using it. People do not want a command surface; they want something they do not have to think about. If you are going to build a chatbot at all, the bar is not "how much can it do" — it is "is this genuinely simple," and those are different goals. More buttons is almost always motion away from the second one.

**The second lesson is sharper, and it is about the product, not the medium.** Boards set out to be a better Saved Messages — a place to throw a thought and have it become a real task. Saved Messages has exactly one virtue, and it is a large one: it is effortless. The moment we loaded the capture flow with epic and status selection, we were no longer competing with Saved Messages on its own terms — we had built something *more complicated than the thing we were trying to replace.* That is fatal, because the only reason to adopt an alternative to a simple tool is that it is at least as simple while doing more. The instant it becomes more work for the core action, the original wins by default. That is not a theory; it is what our two users showed us. **If you build an alternative to a simple tool, it has to stay simple. Lose that, and you have handed the user a reason to stay with the original.**

## What we did instead

The fix was to delete the feature and make the bot as simple as it could possibly be: send a message, the bot saves it, the bot reacts with a 👍, and that is the entire interaction. No epic prompt, no status prompt, no questions. Capture is once again a single frictionless action — indistinguishable in effort from forwarding a message to Saved Messages — except that now it lands in a real system you can organise later, in the web app, where multi-step interaction actually belongs.

There is a second benefit to keeping the chat clean that only became obvious once it was. Because each capture stays a plain message — not buried under the bot's prompts, menus, and selection replies — the chat remains a readable stream of exactly what you saved. That means Telegram's own search works over it. If you just want to find that thing you noted last week, you can search the chat directly, the same way you would search Saved Messages, without opening the Mini App at all. The cluttered version had broken this too: a conversation full of "which epic?" / "which status?" exchanges is not something you can usefully search. Doing less in the chat did not just lower the friction of capture — it preserved the chat as a fast, searchable record, and kept the Mini App for when you actually need it.

This is the decision that, in hindsight, looks obvious across the whole series: capture in the bot, management in the web app. It did not look obvious from the inside. From the inside it looked like we were leaving an easy feature on the table by *not* letting people organise from chat. It took building that feature and watching real users route around it — back to plain Saved Messages — to see that the split was not a limitation. It was the product.

## The idea we are keeping for later

It would be easy to overcorrect into a different dogma: "never let users manage from chat." That is not quite right either. There is a real, if rare, moment where someone *does* want to set the epic or status of the thing they just sent, right then, without opening the app — and serving that well is not the same as forcing the questionnaire on everyone.

The idea we are holding for the future is **context buttons** — actions attached to the *last* message, available in the moment but never demanded. You send a thought; it is saved; the bot does not interrogate you. But if you *want* to file it right now, the option is right there, tied to that message, and harmlessly ignored if you do not. That keeps the default path frictionless — the Saved Messages virtue intact — while leaving a quiet door open for the occasional case where in-chat management genuinely helps. The difference between that and the first version is the difference between an option and an obligation. The first version made everyone pay for a feature few wanted, on every single capture; a contextual version lets the few who want it reach for it and lets everyone else type and move on.

## The rest of the feedback, and where it points

The simplicity lesson was the loudest thing the feedback taught us, but it was not the only thing. Putting the app in front of strangers surfaced a handful of other signals, and they are worth recording because each one points at something later in the build.

The most striking pattern was about trust. More than one person, hearing that the app stores the things you throw into it, hesitated for the same reason: Saved Messages feels private, and a third-party app does not. People did not want to hand their half-formed thoughts and personal notes to someone else's server. The suggestions that followed were consistent — make it open-source, or let people self-host their own instance — so that the data never has to leave hands they trust. That is a real concern and a real direction, and it shapes decisions later in this series about how the app is distributed and where data lives.

Smaller notes pointed at concrete gaps. The interface was dark, and someone asked for a light theme — a reminder that "looks fine to me" is not the same as "works for everyone." And someone managed to break the bot outright: they tried to save an image instead of a text message, and the logs lit up with a `500`. The bot, at that point, only understood text. That crash is not just a bug to fix — it is the entire premise of the next article, because the moment real people touched the app they immediately tried to save the things real people save, and images were first in line.

None of these reshaped the product the way the simplicity lesson did, but together they did something just as useful: they turned "what should we build next" from a guess into a list written by actual users.

## What comes next

The next article picks up exactly where that `500` left off. People wanted to save more than text — images first — and the bot could not yet handle it. So the next step is teaching the bot to capture media, which also means bringing back a piece of infrastructure deferred earlier in the series: real file storage. And it gets built under the standing constraint this article leaves behind — does this add friction to the one thing that has to stay effortless? Saving a photo has to be exactly as frictionless as saving a line of text: forward it, done.