---
title: Saving media from a Telegram bot — store the preview, stream the original
description: Part 11 of building a Telegram task tracker solo. Teaching the capture bot to handle photos and video, and the storage decision behind it — keep small previews on disk, stream full files straight from Telegram on demand without buffering them in memory, using nginx range-request passthrough, instead of re-hosting everything.
type: article
createdAt: 2026-06-26 09:00
updatedAt: 2026-06-26 09:00
projects: [boards]
tags: [dotnet, aspnet-core, telegram-bot, file-storage, nginx, streaming, devlog]
previousLink: telegram-saved-messages-bot-lesson
nextLink: telegram-media-group-album-bot
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 11.
> The [previous article](telegram-saved-messages-bot-lesson) ended with a real user breaking the bot by sending it an image. This article fixes exactly that — and the fix forces a storage decision worth thinking about.

The previous article closed on a `500` in the logs: someone tried to save an image instead of a text message, and the bot, which only understood text, fell over. That crash is the agenda for this one. The moment real people touched the app they tried to save the things real people save — photos, screenshots, the occasional video — and the bot had to learn to handle them. Parsing a photo message turns out to be the easy part. The decision worth the article is what to *do* with the file once you have it, because the obvious answer — keep a copy — is more expensive than it looks.

## The constraint from last time still holds

The rule the previous article left behind applies here without exception: saving a photo has to be as frictionless as saving a line of text. Forward an image, the bot saves it, reacts with a 👍, done — no "which board," no caption prompt, no questions. All of the complexity below lives on the storage side, invisible to the person sending the photo. The capture experience for media is identical to text on purpose.

## The mapping middleware: Telegram's types into ours

Handling media starts where all bot input starts — the message-handling middleware first introduced back in the [first backend iteration](clean-dotnet-telegram-bot-architecture). It has grown up since then. Its job now is to take the many shapes a Telegram update can arrive in and map each one into a small set of requests the app understands ([`HandleAllMessagesMiddleware`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.TelegramHost/HandleAllMessagesMiddleware.cs)):

```csharp
SaveMessageTelegramRequest? request = message.Type switch
{
    MessageType.Text => GetMessageRequest(message),
    MessageType.Photo => GetPhotoRequest(message),
    MessageType.Video => GetVideoRequest(message),
    MessageType.Animation => GetAnimationRequest(message),
    _ => null
};

if (request is not null)
{
    await telegramMessageService.HandleSaveMessage(request, ct);
    context.SetExecutedRoute(
        new ExecutedRouteInfo("HandleAllMessagesMiddleware", text));
}
else
{
    await botClient.SendMessage(
        context.Update.GetUserId(),
        string.Format(Phrases.MessageTypeIsNotAvailable, message.Type),
        cancellationToken: ct);
}
```

Two things here matter beyond the plumbing.

The first is that this is where the original crash is actually fixed — and not by handling every possible type, but by handling the *unhandled* case gracefully. If a message type has no mapping (`_ => null`), the bot no longer throws a `500` into the logs; it replies with a friendly "this message type isn't supported yet." Robust input handling is not "support everything"; it is "never fall over on the thing you did not anticipate." The image that broke the bot is now a supported type, but the safety net underneath it is the more important change.

The second is a small but deliberate design decision: **the app defines its own media vocabulary and translates Telegram into it, rather than mirroring Telegram's.** Telegram distinguishes a video from an *animation* (a GIF) — they are different message types with different fields. The app does not care about that distinction: an animation, to us, is just a video. So `GetAnimationRequest` builds the very same `SaveVideoMessageTelegramRequest` that `GetVideoRequest` does. The mapping layer is where Telegram's taxonomy gets collapsed into the app's simpler one, so that nothing downstream has to know or care that a GIF and an MP4 arrived as different Telegram types. Owning your own model at the boundary — instead of letting an external API's categories leak through your whole system — is a habit worth keeping; it means a change in how Telegram classifies things stops at this one file.

The middleware hands the mapped request to `HandleSaveMessage`, and it is worth a word on why that is a different method from the `Save` that does the database work — they live in two different classes on purpose. [`Save`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.TelegramServices/Services/Messages/TelegramSaveMessageService.cs) (in the save service) is the data operation; [`HandleSaveMessage`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.TelegramServices/Services/Messages/TelegramMessageService.cs) (in a thin `TelegramMessageService` that wraps it) is the orchestration around it, and its job after saving is to give the user the only feedback they ever see: a reaction on their own message. A freshly captured message gets a 👍, and that reaction *is* the entire confirmation — no "saved!" reply, no menu, nothing to dismiss — which is exactly the frictionless capture the previous article argued for. The split keeps the data layer ignorant of Telegram (it just saves and reports what happened) while the Telegram-aware layer translates that result into the feedback the user sees. (It also reacts differently when a save turns out to be an *edit* of something already captured — but that path, and everything about handling edited messages, is its own article; here every capture is a new one.)

## The decision: keep a copy, or keep a reference?

Now the choice the whole article is built around. When the bot receives a photo or a video, it has two broad options for what to store.

**Keep a copy.** Download the full file from Telegram and store the bytes on our own server. This is the straightforward "own your data" approach, and it is expensive in exactly the ways that hurt a small, cheap deployment: every file consumes disk on the VPS, storage grows without bound as people save more, and a video can be hundreds of megabytes — re-hosting that on a budget server, to sit untouched most of the time, is hard to justify.

**Keep a reference, and fetch on demand.** Telegram already stores the file — it has to, in order to deliver it. So instead of duplicating it, store only what is needed to *find* it again, and pull the actual bytes from Telegram at the moment they are requested.

Neither extreme is right on its own, and the real answer is a split between them. It is stated most plainly in a comment in the save service, which is essentially the thesis of this article written as a code comment:

> We can't request always from Telegram — static content will make too many calls. And we can't store content always — it takes too much space.

So the rule became: **store the small previews, keep references to the big originals.** Concretely, for an image the bot stores the *thumbnail* locally but not the full-resolution original; for a video it stores the poster thumbnail locally but not the video file. The `GetOrCreateMessageFileId` method takes a single flag that encodes exactly this decision:

```csharp
/// <param name="saveFileToStorage">
/// Store the file directly to storage.
/// If false - then when requesting the file it will be requested directly from TG.
/// We can't request always from tg - static content will make too many calls.
/// And we can't store content always - it takes too much space.
/// </param>
private async Task<Guid> GetOrCreateMessageFileId(
    File file,
    bool saveFileToStorage,
    CancellationToken cancellationToken)
{
    var oldFileData = await context.TelegramFiles
        .Where(x => x.FileUniqueId == file.FileUniqueId)
        .Select(x => new { x.Id })
        .FirstOrDefaultAsync(cancellationToken);

    if (oldFileData is not null)
        return oldFileData.Id;

    if (saveFileToStorage)
    {
        var botFile = await botClient.GetFile(file.FileId, cancellationToken);

        var stream = new MemoryStream();
        await botClient.DownloadFile(botFile, stream, cancellationToken);

        var extension = ExtensionUtility.GetExtension(file.MimeType);
        var filePath = ShardedPathStrategy.GetPath(botFile.FileUniqueId, extension);

        stream.Position = 0;
        await fileStorage.WriteFile(filePath, stream, null, cancellationToken);
    }

    var telegramFile = new TelegramFile
    {
        FileId = file.FileId,
        FileUniqueId = file.FileUniqueId,
        Name = file.FileName,
        Size = file.FileSize,
        MimeType = file.MimeType,
    };

    context.Add(telegramFile);
    await context.SaveChangesAsync(cancellationToken);
    return telegramFile.Id;
}
```

The callers pass `saveFileToStorage: true` for thumbnails and `false` for originals — that one boolean is the whole policy. When it is `true`, the file is downloaded from Telegram once and written to local storage. When it is `false`, nothing is downloaded; only a reference row is created.

The local writing and reading go through a small [`IFileStorage`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.Services/FileStorage.cs) abstraction, with `FileStorageOptions` carrying the one thing it needs — the directory to write under:

```csharp
public interface IFileStorage
{
    Task<bool> FileExists(string path, CancellationToken cancellationToken = default);
    Task<FileStream> ReadFile(string path, CancellationToken cancellationToken = default);
    Task WriteFile(
        string path,
        Stream content,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}

public class FileStorageOptions
{
    public required string FilesDirectory { get; set; }
}
```

The implementation is a thin wrapper over the local disk: every path is combined with `FilesDirectory` to get a physical location, `WriteFile` creates the directory if needed and copies the incoming stream to a file (with `CopyToAsync`, so even the write does not buffer the whole thing), and `ReadFile` opens the file back as a stream. It is deliberately a plain interface, not tied to local disk — `WriteFile` even takes optional metadata it does not use yet — so that if previews ever need to move to object storage like S3, only this one class changes and nothing that calls it does. For now the cheapest possible backing store, the server's own filesystem, is exactly right.

That reference is a [`TelegramFile`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.DataAccess/Models/TelegramFile.cs) — a small row holding everything needed to locate the file in Telegram later (`FileId`), deduplicate it (`FileUniqueId`), and describe it (size, name, MIME type). Every stored file, whether its bytes live on our disk or only in Telegram, gets one of these rows, and the rest of the system refers to files by this row's `Guid` rather than by anything Telegram-specific. The dedup is worth noting: the method first looks up the file by `FileUniqueId` and, if it has seen it before, returns the existing row instead of storing or referencing it twice. Forward the same image to the bot twice and it is only stored once.

## Two ways files come back out

The split storage model produces two distinct read paths, and the reason for the whole design is that they have very different shapes.

**Loading a board** means showing potentially many items at once — a column full of captured photos, each needing a preview. If every one of those previews required a round-trip to Telegram, opening a board would fire dozens of Telegram API calls and feel slow, and Telegram would not thank you for it. This is exactly the "can't always request from Telegram" half of the problem. Because the thumbnails were stored locally, the board reads their bytes straight from our own disk — fast, local, no external calls, however many previews are on screen.

Each issue returned to the frontend carries its media as a list, not the bytes themselves but references to them:

```csharp
public record IssueListDto : ICanContainMedia
{
    // ...
    public List<MediaInfo> Media { get; set; } = [];
}

public class MediaInfo
{
    public Guid? PreviewFileId { get; set; }
    public Guid? OriginalFileId { get; set; }
    public MediaType Type { get; set; }
}

public enum MediaType
{
    Photo,
    Video,
}
```

Each `MediaInfo` carries two references — a `PreviewFileId` and an `OriginalFileId`, both pointing at `TelegramFile` rows by their `Guid`. That is the two-path model surfaced in the DTO: the frontend uses the preview reference to show the thumbnail on the board (served from local disk) and the original reference only if the user opens the item (streamed from Telegram). The split is visible all the way out to the API contract.

One implementation detail is worth calling out, because it is a deliberate choice rather than an accident. The media is not joined onto the issues in the main query. Instead, the issues are fetched first, and then a separate step *enriches* them with their media — roughly `issues = GetIssues(request)` followed by `EnrichMedia(issues)`. Pulling photos, videos, media groups, and their file references into the single board query would have made it a tangle of joins and grouping that is hard to read and hard to keep efficient. Fetching the issues plainly and then loading their media in a focused second pass keeps each query simple and comprehensible. It is the same instinct as the rest of the backend: prefer two clear steps over one clever one, unless a measured problem forces the merge.

**Opening a single item** is the opposite situation: one file, deliberately, right now. The user tapped a photo or a video to see the original. That is when the reference is cashed in — the backend takes the stored `FileId` and fetches the full file from Telegram on demand. This is the "can't always store" half: the original never occupied our disk, and it does not need to, because opening originals is a one-at-a-time action, not a bulk one. A [`TelegramFilesController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/TelegramFilesController.cs) serves file content by the `TelegramFile`'s system `Guid`, resolving it either to local bytes or to a Telegram fetch depending on what was stored.

The Telegram fetch is worth being precise about, because there is a well-known limit lurking nearby. The Bot API's `GetFile`/`DownloadFile` pair — the one used above to *store* thumbnails — can only download files up to 20 MB. That ceiling never bites the stored path, because thumbnails are tiny. For originals, the controller does not use that pair at all: it resolves the file's path through `GetFile` and then serves the content via Telegram's **direct file URL**, `https://api.telegram.org/file/bot{botToken}/{filePath}`. That URL points straight at Telegram's file storage, so the bytes flow from there rather than being pulled through the bot's `DownloadFile` call — which is why, in practice, the 20 MB download ceiling has not been a problem for streaming full videos. (If it ever does become one, the next step up is hosting a local Bot API server, which removes the limit; that has not been necessary yet.)

The two pressures from the code comment map cleanly onto these two paths: previews are bulk and frequent, so they are local; originals are large and rare, so they are streamed. The design is just those two sentences turned into structure.

## Streaming the original without holding it in memory

Fetching a large video from Telegram and handing it to the user raises one more problem worth getting right: you do not want the server to load the entire file into memory before sending it on. A handful of users each opening a few-hundred-megabyte video at once would be enough to exhaust a cheap VPS's RAM. The original has to be *streamed* — bytes flowing from Telegram, through the backend, to the client, without ever being fully buffered on the server.

Most of the work for this is done in nginx, in the `location` block that handles the file route:

```nginx
location ^~ /api/notes-board/telegram-files {
    set $upstream http://structuredmessageswebapihost:5007;
    rewrite ^/api/notes-board/(.*)$ /api/$1 break;
    proxy_pass              $upstream;
    proxy_buffering         off;
    proxy_cache             off;
    proxy_set_header        Range $http_range;
    proxy_set_header        If-Range $http_if_range;
    proxy_pass_header       Content-Range;
    proxy_pass_header       Accept-Ranges;
    gzip                    off;
    proxy_read_timeout      10m;
    proxy_send_timeout      10m;
    proxy_next_upstream error timeout http_502 http_503 http_504;
    proxy_next_upstream_tries 5;
    proxy_next_upstream_timeout 30s;
}
```

The important lines are the ones that turn nginx from a buffering proxy into a pass-through pipe. `proxy_buffering off` tells nginx not to accumulate the upstream response before forwarding it — bytes are passed along as they arrive, so neither nginx nor the backend ever holds the whole file. The `Range` and `If-Range` headers are forwarded upstream, and `Content-Range` and `Accept-Ranges` are passed back, which is what makes **range requests** work: a video player can ask for just the slice of the file it needs to start playing, and seek to the middle without downloading everything before it. That is why a saved video can begin playing almost immediately and scrub smoothly, rather than downloading in full first. The long `proxy_read_timeout` and `proxy_send_timeout` keep slow, large transfers from being cut off mid-stream, and `gzip off` avoids wasting effort trying to compress already-compressed media.

nginx can only pass those range headers through because the backend produces them in the first place. The [`TelegramFilesController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/TelegramFilesController.cs) does not implement range logic itself — it forwards the browser's `Range` header to Telegram and then relays Telegram's response back faithfully, including the status code and the headers that describe the byte range:

```csharp
Response.StatusCode = (int)telegramResponse.StatusCode;
Response.Headers.Append("Accept-Ranges", "bytes");

// Content-Length comes from content headers
if (telegramResponse.Content.Headers.ContentLength is { } contentLength)
    Response.Headers.ContentLength = contentLength;

// Content-Range is also a content header on 206 responses
if (telegramResponse.Content.Headers.ContentRange is { } contentRange)
    Response.Headers.Append("Content-Range", contentRange.ToString());

var stream = await telegramResponse.Content.ReadAsStreamAsync(cancellationToken);

// enableRangeProcessing: false — we've already handled the range manually
// by forwarding it to Telegram. ASP.NET Core must not try to slice again.
return File(stream, mimeType, enableRangeProcessing: false);
```

Two details make this correct. First, when the browser requests a range, Telegram answers with a `206 Partial Content` and a `Content-Range` header describing which slice it sent — and the controller passes both straight through, so the status code and range information the browser receives are the ones Telegram actually produced. Second, and easy to get wrong: `File(stream, mimeType, enableRangeProcessing: false)`. ASP.NET Core's `File` result can do range processing on its own, but it must *not* here — the range was already handled by forwarding it to Telegram, and the stream coming back is already the correct slice. Leaving range processing on would make ASP.NET Core try to slice an already-sliced stream, corrupting the response. Turning it off tells the framework to stream the bytes through verbatim. The controller is a faithful relay, not a second range processor — and because it reads Telegram's response as a stream and returns it directly, the bytes flow through without the file ever being buffered whole in the backend either.

The result is that a large original streams through the whole chain — Telegram to backend to nginx to the browser — without any link in it buffering the file whole. The previews made the board cheap to load; this makes the originals cheap to serve, even when they are large.

## How the frontend uses the two references

The payoff of the two-reference `MediaInfo` is visible on the frontend, where the same design drives two different behaviours through one endpoint. Both `previewFileId` and `originalFileId` are just system `Guid`s, and both are turned into URLs by the same helper — `getImageUrl(guid)` — which points at the `TelegramFilesController`. The frontend never knows or cares whether a given file lives on our disk or in Telegram; it asks for the file by its `Guid`, and the backend resolves that to local bytes or a Telegram stream. The split that started as a storage decision is, by this point, completely invisible to the client.

On a card, only the previews are used. The [`LnbCard`](https://github.com/win7user10/laraue-boards/blob/master/app/components/LnbCard.vue) component renders each issue's media as small thumbnails, capped at four with a "+N" overflow, and each thumbnail's `src` is `getImageUrl(mediaInfo.previewFileId)` — the locally-stored preview. A video preview gets a play-icon overlay so it reads as playable, but it is still just the thumbnail; no video is loaded yet. A board full of cards therefore loads nothing but small local images, however many items are on it.

The original is only fetched when the user actually opens something. Clicking a thumbnail calls into shared state — `openMedia(media, index)` — which records the opened media list and the index, and the [`LnbMediaViewer`](https://github.com/win7user10/laraue-boards/blob/master/app/pages/organizations/%5BorgKey%5D.vue) renders the full item. Now `originalFileId` comes into play: an image element points its `src` at the original, and a video element does the same but adds `controls`, `playsinline`, and crucially a `poster` set to the *preview* — so the thumbnail shows instantly while the original streams in behind it, with `preload="metadata"` so the browser fetches only what it needs to start. That `<video>` element hitting the original's URL is what triggers the whole streaming path from the previous section: the range requests, the no-buffering proxy, the direct fetch from Telegram. The thumbnail you were already looking at on the card becomes the poster of the video you are now streaming — preview and original, the two halves of the model, sitting in the same element.

## Bringing back the storage volume

There is one piece of infrastructure this needs that was deliberately deferred earlier. Back in the [deploy article](deploying-dotnet-postgres-vps-docker-compose), the Docker Compose stack had a storage volume that was removed, with a note that it would return once there was something to store. That moment is now — the locally-stored previews have to live somewhere that survives container restarts.

So the volume comes back. The web API service gets a bind to a named `storage` volume:

```yaml
- storage:/home/laraue/storage
```

and the `FilesDirectory` from the `FileStorageOptions` shown earlier is pointed there through configuration, supplied to the container by an environment variable — the same pattern used for every other setting in the stack:

```yaml
FileStorageOptions__FilesDirectory: "/home/storage/note-board"
```

That is the whole storage footprint on the server: a single volume holding the previews, an environment variable telling the app where to put them. The originals are not here — they were never downloaded — so the disk this volume consumes grows only with the small thumbnails, not with full-resolution photos and videos. The deferred volume returning is the last piece: previews persist across restarts, originals stay in Telegram, and the cheap deployment stays cheap.

## Where this leaves us

The crash that opened this article is gone, and in fixing it the bot learned to capture photos, videos, and GIFs as frictionlessly as text — forward it, 👍, done. Underneath that unchanged-looking capture, the storage model does real work: small previews stored locally so a board full of images loads fast from our own disk, full originals left in Telegram and streamed on demand so the server never fills up with files it rarely serves, range-based streaming so even large videos play without buffering the whole thing anywhere. The two sentences from that code comment — can't always call Telegram, can't always store — turned out to contain the entire design.

## What comes next

There is a thread left dangling in the save logic here: what happens when someone *edits* a message they already sent? The handling code already quietly accounts for it, but the story of updating an existing capture — and the wrinkles Telegram introduces around edited and grouped messages — is its own article. That is where the next one goes.