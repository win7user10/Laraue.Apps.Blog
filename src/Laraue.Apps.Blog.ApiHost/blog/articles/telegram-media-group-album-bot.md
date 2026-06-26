---
title: One card from a Telegram album — handling media groups and edits in a bot, without timeout hacks
description: Part 12 of building a Telegram task tracker solo. Telegram delivers an album as a burst of separate messages and an edit as a fresh update — here is how to turn a media group into a single record without the usual timeout accumulator, and how to treat an edit as an update instead of a duplicate.
type: article
createdAt: 2026-06-26 15:00
updatedAt: 2026-06-26 15:00
projects: [boards]
tags: [dotnet, telegram-bot, media-groups, devlog]
previousLink: telegram-bot-file-storage-stream
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 12.
> The [previous article](telegram-bot-file-storage-stream) taught the bot to capture media and left one thread dangling: a captured message is not frozen. People edit what they sent, and they send albums. This article handles both.

The capture flow so far has quietly assumed something that is not true: that a message arrives once, in one piece, and never changes. Real Telegram usage breaks both halves of that assumption. People edit a message after sending it — fixing a typo, adding a line — and they send several photos at once as an album, which Telegram delivers not as one message but as a burst of separate ones. A capture system that ignores either case does the wrong thing: it creates a duplicate card for an edit, or scatters one album across several cards. This article is about getting both right, and the second one is genuinely awkward — awkward enough that the usual answer reaches for an in-memory timer, which this implementation manages to avoid.

## An edit is not a new message

Start with the simpler half. When someone edits a message they already sent to the bot, Telegram delivers an *edited message* update — and the bot has to recognise it as a change to something it already saved, not as a brand-new capture.

The groundwork for this was laid back in the [previous article](telegram-bot-file-storage-stream)'s middleware ([`HandleAllMessagesMiddleware`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.TelegramHost/HandleAllMessagesMiddleware.cs)), which allows two update types, not one:

```csharp
private static readonly UpdateType[] AllowedUpdates =
[
    UpdateType.Message,
    UpdateType.EditedMessage,
];
```

and reads whichever one arrived:

```csharp
var message = context.Update.Message ?? context.Update.EditedMessage;
```

So new messages and edited messages flow through *exactly the same* mapping and save path — an edited photo is still mapped by `GetPhotoRequest`, an edited text by `GetMessageRequest`, and so on. The middleware does not branch on "is this an edit"; it treats both uniformly and lets the save service work out whether what it received is new or a change. That is deliberate: the decision of new-versus-update belongs with the code that can actually look in the database and check, not with the code reading the update type. The update type tells you Telegram *calls* this an edit; only a lookup tells you whether *you* already have it.

## Update the card, do not create a new one

The single-message case is where the create-or-update logic lives. Before doing anything, the save service looks for an existing record by the message's external identity — the Telegram message id plus the chat it came from:

```csharp
var savedMessage = await context.TelegramMessages
    .Where(x => x.ExternalMessageId == request.ExternalMessageId)
    .Where(x => x.ExternalChatId == request.ExternalUserId)
    .Select(x => new
    {
        IssueId = x.Issue != null ? (long?)x.Issue.Id : null,
        x.Id,
    })
    .FirstOrDefaultAsync(cancellationToken);
```

From there the logic forks on whether a card already exists for this message. If there is no saved message yet, or it has no issue attached, this is a genuine new capture: the message row is created if needed, and a card is created inside a transaction, returning a `MainMessageCreated` result.

```csharp
if (savedMessage?.IssueId is null)
{
    var statusId = await GetStatusIdToSaveMessage(request.UserId, cancellationToken);

    await using var transaction = await context.Database
        .BeginTransactionAsync(cancellationToken);

    // create the TelegramMessage row if it does not exist,
    // then create the issue/card for it
    await coreIssuesService.Create(
        new CreateIssueRequest
        {
            CreatedAt = request.SentAt,
            Text = request.Text,
            TelegramMessageId = messageId,
            StatusId = statusId,
            UserId = request.UserId,
        }, cancellationToken);

    await transaction.CommitAsync(cancellationToken);

    return new GetOrCreateMessageResult
    {
        Result = Result.MainMessageCreated,
        TelegramMessageId = messageId
    };
}
```

But if the message is already there and already has a card, the request is an *edit* of something captured before. There is no new card; the existing one's content is updated in place:

```csharp
await context.Issues
    .Where(x => x.TelegramMessageId == savedMessage.Id)
    .ExecuteUpdateAsync(upd => upd
        .SetProperty(x => x.Content, request.Text),
        cancellationToken);

return new GetOrCreateMessageResult
{
    Result = Result.MainMessageUpdated,
    TelegramMessageId = savedMessage.Id,
};
```

That is the whole edit mechanism for a single message: look it up, and either create a card or update the one that exists. The `ExecuteUpdateAsync` writes the new text straight to the existing card without loading and re-saving the entity. (`GetStatusIdToSaveMessage` resolves the default place a freshly captured message lands in; the details of how that default is chosen belong to a later article and are skipped here.) What matters is the result the method returns — `MainMessageCreated` or `MainMessageUpdated` — because that is what the user actually sees.

It is worth being precise about what `MainMessageUpdated` really means, because it is broader than "an edit." It means *this message attached to a card that already existed*, rather than creating a new one — and that happens in two different situations. One is a genuine edit, as above. The other is the second, third, and later photos of an album: as the next section explains, Telegram sends each image of a multi-photo message as a *separate* message, and merging them into a single card is the backend's job. Those later parts are not edits in any user's sense, but mechanically they take the same path — they find an existing card and attach to it rather than creating one. So `MainMessageUpdated` is really "merged into something that was already there," whether that something was created a moment ago by the first photo of the same album, or yesterday by a message the user just edited. The single create-or-update mechanism handles both, which is part of why it is built around a lookup rather than around the update type.

## The reaction tells the user which happened

The capture flow gives the user exactly one piece of feedback, and it is silent: a reaction on their own message. The orchestration layer sets it based on the result the save returned:

```csharp
var result = await saveMessageService.Save(request, cancellationToken);

if (result.Result is Result.MainMessageCreated)
    await SetReaction(request, "👍", cancellationToken);
else if (result.Result is Result.MainMessageUpdated)
    await SetReaction(request, "❤", cancellationToken);
```

A capture that created a new card gets a 👍; one that merged into an existing card gets a ❤ — whether that was an edit or a later part of an album. That difference is the entire user-visible surface of this feature — no "updated" message, no confirmation dialog, nothing to dismiss. The user edits their message and sees the reaction quietly settle on a heart, which is enough to know the system noticed. It keeps the zero-friction promise from the [Saved Messages lesson](telegram-saved-messages-bot-lesson): the bot acknowledges, it does not interrupt. Setting the reaction itself is a single Bot API call:

```csharp
private async Task SetReaction(
    SaveMessageTelegramRequest request,
    string? reaction,
    CancellationToken ct)
{
    await client.SetMessageReaction(
        request.ExternalUserId,
        request.ExternalMessageId,
        reaction is not null
            ? [new ReactionTypeEmoji { Emoji = reaction }]
            : [],
        cancellationToken: ct);
}
```

There was a tempting idea here that did not survive contact with the API: cycling the reaction on *every* edit, so repeated edits would visibly step through different emoji. Telegram does not offer a way to read a message's current reaction set, so doing this would mean storing the current emoji for each message ourselves, purely to know what to change it to next. That is real state to maintain for a cosmetic touch, so the feature stays at two states — created and updated — which need no stored history because the save result already tells you which one applies. It is a small example of letting an API's limitation talk you out of a feature that was not worth its cost anyway.

## The difficult case: media groups

Now the awkward half. When a user sends several photos at once as an album, Telegram does not deliver one message containing several photos. It delivers *several separate messages* that happen to share a **media group id**, arriving in a quick burst with no guaranteed order. Turning that burst back into a single card is the hardest piece of the capture logic, and it is handled in its own method, [`SaveGroupMessageEntity`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.TelegramServices/Services/Messages/TelegramSaveMessageService.cs) — the rest of this section describes its shape rather than walking every line, because the line-by-line detail is in the source.

The core problem is a mismatch in counting. To the user, an album is one thing they sent. To Telegram, it is N messages. To the app, it should be one card with N attachments. So the save service routes group messages differently from single ones:

```csharp
private Task<GetOrCreateMessageResult> SaveMessageEntity(
    SaveMessageTelegramRequest request,
    CancellationToken cancellationToken)
{
    return request.MediaGroupId == null
        ? SaveSingleMessageEntity(request, cancellationToken)
        : SaveGroupMessageEntity(request, cancellationToken);
}
```

The common solution to this problem, the one you find in most bot frameworks' forums, is a **timeout accumulator**: when a message with a media group id arrives, buffer it in memory, start (or reset) a short timer, and when the timer finally fires without new parts arriving, assume the album is complete and process the whole buffer at once. It works, but it is fragile in ways that matter. It holds state in memory, so a restart mid-album loses the parts that already arrived. The timer is a guess — too short and you split one album into several, too long and every album feels laggy. And it assumes all parts arrive close together, which is usually but not always true.

This implementation avoids the timer entirely by leaning on the database instead of memory. There is no "wait for the album to finish" step at all; each part is handled as it arrives, and the persistence layer is what ties them together. That rests on a few ideas. First, the media group gets its own database row, created once and reused, so that all the separate messages of one album can be tied to a single group identity — that is what `GetOrCreateTelegramMediaGroupId` does: look the group up by its Telegram id, create it if it is the first part to arrive, return it otherwise. Second, the rule for which message owns the card: **only the first message of the group carries the content and creates the card.** The later parts of the album do not each make their own card; they find the group's existing card and attach their media to it. Since the parts arrive in no fixed order, "first" is decided by querying the group's stored messages and ordering them, not by trusting arrival order or a timer. Because all of this lives in the database rather than an in-memory buffer, a restart in the middle of an album loses nothing — the parts already processed are saved, and the ones still coming will find them.

That leaves the edge cases, which is where it gets genuinely fiddly and where the code carries honest `TODO`s. One real example handled in the code: the first message of a group — the one that was holding the text content — gets deleted, and the user adds the text to a different part instead. Now the part that is supposed to own the card no longer exists, and the content has moved. The code detects this and updates the surviving card's content from the part that now has the text:

```csharp
// The case when first message was deleted and text added to the second
if (request.Text is not null && firstGroupMessageData is not null)
{
    // TODO - here we can detect and remove previous messages. But should we?
    await context.Issues
        .Where(x => x.Id == firstGroupMessageData.CardId)
        .ExecuteUpdateAsync(upd => upd
                .SetProperty(x => x.Content, request.Text),
            cancellationToken);
    // ...
}
```

The `TODO` is honest about the limit: there are deeper consistency questions here ("should we detect and remove previous messages?") that are noted but not solved, because the cases that trigger them are rare and the cost of handling every one is high. The album path handles the common shapes well and marks the exotic ones for a someday that may never need to come.

## The thing the bot cannot do: deletions

Edits the bot can see. Deletions it cannot. Telegram does not deliver an update to a bot when a user deletes a message from their chat — there is simply no event for it — so the bot has no way to know that something it captured was removed on the Telegram side. As a result, deleting a captured card is supported only from the web interface, not from the chat.

It is worth sitting with *why* Telegram does not send that event, because the ambiguity turns out to be real rather than an oversight. Consider what should happen when a user deletes their entire chat history with the bot. Should that fire a delete for every single message, wiping out everything they ever captured? That is almost certainly not what they meant — clearing a chat is a chat-cleanup gesture, not "destroy all my saved tasks." But the opposite default, ignoring it, is also defensible. There is no reading of "the user deleted this message" that is unambiguously correct for a system that captured the message into something more durable, so Telegram declines to guess. Given that, doing deletion in the web app — where "delete this card" means exactly and only that — is the right place for it anyway.

There is a possible future middle ground: an explicit in-chat gesture that is unambiguous *because* the user chose it — for instance, reacting to a captured message with a particular emoji to tell the bot to delete the corresponding card. That keeps deletion available in the chat without inheriting the ambiguity of raw message-deletion, because the user is deliberately signalling intent rather than the bot guessing from an absence. Whether it is actually comfortable to use is an open question — reacting-to-delete is the kind of thing that sounds neat and might feel awkward in practice — so it stays an idea rather than a feature for now.

## Why this is fiddly, and what it teaches

The reason this article exists is that Telegram's model of a message and the app's model of a captured card do not line up. Telegram says an edit is a new update; the app needs it to be a change to an existing card. Telegram says an album is a burst of separate messages; the app needs it to be one card. Telegram says nothing at all when a message is deleted; the app has to live with that silence. The save service is the seam where that mismatch is reconciled — where the messy, external reality of how Telegram delivers things is translated into the clean, internal reality the rest of the app gets to assume.

That is the same theme as the previous article's mapping middleware, where an animation and a video collapsed into one app concept. Here it is edits and albums collapsing into "create or update a card." In both cases the discipline is the same: do the reconciling *at the boundary*, in one place, so that everything downstream gets to work with a simple model and never has to know how irregular the input was. The cost is that this one place is not simple — the save service is the most intricate code in the bot, precisely because it absorbs all the irregularity so nothing else has to.

And it is shipped pragmatically, not perfectly. The media-group handling has rough edges and marked `TODO`s; the common cases work, the rare ones are flagged. That is the scale-appropriate call again: handle what real users actually do, and do not spend days hardening paths that almost no one will hit, when those days can build something people will.

## Where this leaves us

The bot now handles the full reality of how people actually send things to it: text and media, sent once or edited afterward, one at a time or as an album. None of it changed the experience of *using* the bot — capture is still send-and-forget, the only feedback a quiet reaction that now distinguishes a new save from an edit. All the complexity went where complexity belongs: server-side, at the boundary, invisible to the person sending a photo. With this, the capture half of the product — the bot — is genuinely done. It does one thing, captures whatever you throw at it, and stays out of the way.

## What comes next

Which raises a question worth sitting with. The bot is finished, and almost everything interesting from here on lives in the web app — the boards, the organising, the actual managing of what was captured. And working on it surfaced a realisation about the whole architecture: Telegram, for all that this is a "Telegram-native" product, is really only needed for one thing — logging in. Nearly everything else the app does could work without Telegram at all. The next article turns fully to the web app, and to what it means to have built a Telegram product that barely depends on Telegram.