---
title: Why reviewing AI-generated code is often more expensive than writing it yourself
description: The modern approach to development increasingly comes down to one claim — AI should write the code, and the developer just steers it and checks the results. The fact that reviewing generated code well enough to trust it is often slower than writing it yourself gets quietly left out.
type: article
createdAt: 2026-07-11
updatedAt: 2026-07-11
tags: [ai-workflow, code-review, backend, engineering, development]
previousLink: how-we-build-engineering-principles
---

In the era of AI agents spreading everywhere, a common opinion is that AI will always write code faster than a developer would by hand. If you compare the raw number of lines written, that is absolutely true. The only problem is that such code will not always be fully correct.

For many tasks — prototypes, one-off scripts, pieces of boilerplate — slightly incorrect code is not a problem. A person sees the mistake and asks for the prototype to be updated. A script exits with an error — the error is handed to the AI and quickly fixed.

It is worse when the mistakes show up in so-called edge cases. They are not errors from the program's point of view — it is just that someone suddenly becomes able to do something they should not. If such a mistake happens on the frontend, the user might, for example, see a section that should be unavailable to them, or click a button that should be disabled. Unpleasant, but not critical — in a properly designed system the interface holds no data and requests it from the server, so in that opened section the user will not see any protected data, as long as the backend checks permissions.

In the bad case, mistakes like this can appear on the backend. For example, an ordinary user could grant themselves admin rights because a check is missing in a method. Or see data that does not belong to them. The variations are endless.

## But people make mistakes too

Nobody is immune to mistakes when writing code — even the most experienced engineers make them. Over the years, development practices were refined until they evolved into a scheme with mandatory review by a person who did not work on the task itself. This worked well, because there was some rotation among developers: one writes, another reviews, then they swap. Each stays in the context of why the decisions were made the way they were.

## A person cannot review code all day long

In the AI era, some companies decided the developer's role had changed, and that writing code should no longer be part of their responsibilities (since they write it too slowly). Now the engineer should instead be responsible for setting tasks for the agents correctly and overseeing their correct execution.

The trouble is that this creates a problem. When writing by hand, a developer constantly looks at the code being produced and asks themselves how it will work, what happens if an object turns out to be `null`, whether it can be `null` at all, and so on. They write code, notice that they have seen this somewhere before, ask the business questions, refactor, write on. They know why each branch appeared, which inputs are possible, and why the edge cases are handled the way they are — a great many questions got resolved in the process of writing.

But if the developer now only reads someone else's code, everything changes. The intent has to be reconstructed from the code alone, without the reasoning that produced it. The AI wrote plausible code and threw away all the reasoning that stood behind it.

Making sure an unfamiliar function handles every case is a much harder task than writing it yourself. The brain has a hard time reverse-engineering code that shows up in the review window as plain text. Functions call functions written earlier; the reviewer has to either trust the AI that they are called correctly, or go into the source of those methods to make sure the calls are right.

> **AI code looks correct — and that is the problem.**
> AI-generated code usually compiles, works correctly in the simplest scenarios, and reads decently. That is, the problem is not that the AI writes code badly, but that it writes it very well. Surface plausibility invites a shallow review.

## The mistakes are completely random, attention cannot be relaxed

It cannot be claimed that review had no problems before AI. But a human has a notion of accountability and of learning: over time they make fewer mistakes, and the reviewer knows a full check of the entire PR is not needed. With AI it is not like that. An extremely complex function may be implemented perfectly, but only because it was in the training dataset. Meanwhile it is easy to catch a mistake in trivial code. In other words, the reviewer cannot be sure of anything; their concentration is always at its limit.

That is, the model can correctly implement a genuinely hard case with the correct transaction ordering and, three lines later, get a `>=` backwards in a comparison a junior would never have gotten wrong.

Human attention is far from an unlimited resource. Not everyone can manage to focus on reviewing PRs for more than a couple of hours a day. So you have to choose — either trust the AI and hope nothing breaks, or watch the number of open AI PRs pile up while the number of shipped features does not grow.

## When choosing AI to write code is justified

None of the above makes AI useless — it simply sets the boundaries of where using it is acceptable. You have to understand clearly what matters more for a given task: doing it fast with a possible loss of quality, or doing it slowly but with more thought given to the details.

For example, we love using AI on the frontend. A bug in a component is visible right away when you look at the screen (this is about cases where the frontend holds no complex logic). Prototyping screens has become much more pleasant: where you used to spend time in Figma to sketch an interface variant, now a couple of lines let you do the same thing in a few minutes.

But on the backend we use AI rarely. A wrong migration, an incorrect permission check, a service method updating the wrong rows — the cost of a mistake is too high. So here we try to use AI only for specific questions. Not "implement function X using the spec," but "why am I getting compilation error Y."

## How to use AI without losing development skills

Developers who stop writing code gradually stop understanding when the AI has done something wrong. The dependency becomes circular: the more you use AI, the fewer hard skills you retain. The fewer hard skills, the harder it is to review properly. In the end, an engineer who used to dig into the essence of the code starts blindly trusting the AI.

So the choice of when to use the intelligent assistant has to be a deliberate one. AI should remove the routine that brought nothing but irritation, while leaving room for creativity, so the engineer can keep growing. This helps reach an optimal balance between development speed and code quality — and keep that balance for a long time.