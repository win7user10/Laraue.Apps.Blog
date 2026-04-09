---
title: Learn Language Bot
type: project
tags: [AI,Ollama,Telegram,.NET,C#]
description: A Telegram bot to learn different languages with quizzes.
createdAt: 2025-11-01
updatedAt: 2025-11-05
---
## Key Features
|                  |                                                                                 |
|------------------|---------------------------------------------------------------------------------|
| Language         | C#                                                                              |
| Framework        | .NET 9                                                                          |
| Project type     | Application (Telegram Web Api + Auto Translator Console App)                    |
| Status           | Active development                                                              |
| License          | MIT                                                                             |
| Github           | [Laraue.LearnLanguage](https://github.com/win7user10/Laraue.Apps.LearnLanguage) |
| Application link | [Learn Language Bot](https://t.me/learn_lang_bot)                               |

## About Language Learning
Learning a new language is a long process in which we try to learn a huge amount of grammar, rules, and words.

But often, you don't necessarily need to learn an entire language to start using it. 80% of everyday speech consists of just 300–500 words.
So before a trip, you can spend a month learning that many words to feel much more comfortable in a new country.

## The Low Vocabulary Problem
To start understanding a language, you need three things:
1. Learn alphabet
2. Understand the basic grammar
3. Have enough vocabulary

The first two things usually can be studied in a limited time. But it's hard to understand how many words are enough
to visit the country. A large number is almost unreal to learn, and a small number may not be enough to understand
anything. There's a good rule: the 80/20 rule. The best idea is to spend 20% of your effort learning 80% of the words
you need. But how do you do it?

## The Application Vision
- The application should allow increasing passive vocabulary using a quiz system to learn words for short and medium-term
memory, generate sentences to put words in long-term memory, and use a motivation system to prevent learners from
stopping.
- The application is designed as a Telegram bot to reduce system support effort and make it available on any
platform.
- Words and translations are stored with the codebase to make it available for pull requests with new
words / translations and to track their history.
- The words to learn are taken from public databases, there was an attempt to collect the top 5000 used English words.
- Translations are made with AI to prevent the time required for database filling
and can be corrected later.
- Words are divided into topics to learn exactly what a person needs.

## Application architecture
- **[AutoTranslatorApp](https://github.com/win7user10/Laraue.Apps.LearnLanguage/tree/master/src/Laraue.Apps.LearnLanguage.AutoTranslator):** The console service that looks for untranslated words in the [translations.json](https://github.com/win7user10/Laraue.Apps.LearnLanguage/blob/master/src/Laraue.Apps.LearnLanguage.DataAccess/translations.json),
and performs sequential translation to the available [languages](https://github.com/win7user10/Laraue.Apps.LearnLanguage/blob/master/src/Laraue.Apps.LearnLanguage.DataAccess/languages.json).
- **[TelegramApiHost](https://github.com/win7user10/Laraue.Apps.LearnLanguage/tree/master/src/Laraue.Apps.LearnLanguage.Host):** handles telegram bot requests using [Telegram.NET](telegram-net) for that.
Some methods are available to all users, some—only to admins.

## Features
### Implemented
- **Learning language:** When entering any mode, the user is asked to select the language pair to learn.
Also, the preferred pair can be selected in settings to avoid this question repeatedly.
- **Quiz mode:** The main mode in the app. The user is proposed to select the correct translation for 20 questions.
The application tries to keep a balance between new words, words recently learned, and words learned in the past.
- **View by topic:** Opportunity to view words grouped by topics.

### Have plans to implement
- **Flexible quiz modes:** Allows narrowing down the word list for the quiz. E.g., adding only specified topics to the quiz.
- **Texts for remembering:** Combines recently learned words in text with AI to allow learning words with context.
- **Top topics to learn:** Try to combine words in groups that will help travelers to visit the specific places e.g.,
airport, public transport, coffee shop lexic and other.

## Timeline
- **Jan 2023** The first app version included a view for the word list and manual buttons to mark words as learned.
- **Jan 2024** The app allowed viewing words by CEFR Level.
- **Feb 2024** The architecture was updated to support multiple language pairs.
- **Jun 2024** Added a console app to automatically fill translations with Google.
- **Aug 2025** Started using AI for automatic translation filling.
- **Sep 2025** Added quiz mode