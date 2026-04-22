---
title: Media attachments — photos and videos in issues
description: How photos and videos from Telegram messages are stored and displayed in Laraue Boards issues. Media viewer, progressive video streaming, and thumbnail strips.
keywords: [telegram photo task, video attachment kanban, media in project management, photo issue tracker, telegram video task board]
type: documentation
project: boards
order: 3
createdAt: 2026-04-22
updatedAt: 2026-04-22
---
When you capture a Telegram message that contains photos or videos, those files become part of the issue. You can view them directly in the board without opening Telegram.

## Media on the board card

Issue cards with media show a horizontal thumbnail strip below the text content. Up to four thumbnails are visible at once. If there are more, the fourth thumbnail shows a `+N` overlay indicating additional files.

Video thumbnails show a play button overlay to distinguish them from photos. The strip is horizontally scrollable on touch screens.

Tapping a thumbnail opens the **media viewer** directly — it does not open the issue detail first.

## The media viewer

The media viewer opens full-screen with a dark background. Navigation controls:

- **← →** arrows to move between files in the issue
- **×** to close
- **Counter** (e.g. `2 / 5`) shown at the top

Photos are displayed at their full resolution, scaled to fit the screen. Videos play with native browser controls — play/pause, seek, volume, and full-screen.

## Progressive video streaming

Videos stream progressively — playback begins immediately without waiting for the full file to download. This uses HTTP range requests: when you seek to a position in the video, only that portion is requested from the server rather than downloading from the beginning.

This makes long videos (meeting recordings, screen captures) practical to use even on mobile connections.

## Media in the issue detail

Opening an issue detail shows a 3-column grid of all media files above the text content. Each grid item is tappable and opens the viewer at that position.

## Media-only issues

An issue can consist entirely of media with no text — for example, a photo of a damaged product with no caption, or a short video showing a bug. These display the thumbnail strip in place of the text area. The card header still shows the sender and source chat for context.

## Storage and access

Media files are proxied through the Laraue Boards server. Files are cached after the first access so subsequent loads are fast. Original files remain on Telegram's servers — Laraue Boards stores a reference to them.

File access follows the same permission model as issues. If a user does not have Read permission for an issue, they cannot access its media files.

## Related pages

- [Capturing Telegram messages](/blog/documentation/laraue-boards/working-alone/telegram-messages)
- [Issues — what they contain](/blog/documentation/laraue-boards/concepts/issues)
