---
title: Creating an organization and inviting your team
description: Step-by-step guide to creating a Laraue Boards organization, generating an invite link, and bringing your team members in.
keywords: [create team workspace telegram, invite team kanban, shared project management telegram, team onboarding task tracker]
type: documentation
project: boards
order: 1
createdAt: 2026-04-22
updatedAt: 2026-08-07
---
An organization lets multiple people share boards and collaborate on the same issues. Setting one up takes under two minutes.

## Step 1 — Create the organization

From the organization selection screen (shown right after login, or reachable anytime from the dropdown at the top of the sidebar), tap **+ Create organization**.

Fill in:
- **Name** — your company, team, or project name
- **Slug** — a URL-safe identifier, auto-suggested from the name
- **Color** — used in the sidebar and the organization switcher

![The Create organization form, with Name, Slug, and Color fields](https://laraue.com/static/images/blog/docs/laraue-boards/create-organization.jpg)

Tap **Create organization**. You are immediately set as the Owner.

The slug you enter isn't used as-is: a random postfix is appended to it to form the organization's actual key, so it can't be guessed.

## Step 2 — Enter the organization

Tap the organization on the selection screen, or pick it from the sidebar dropdown. Any boards or spaces you create here are visible to members you invite, according to the permissions you give them.

![The organization selection screen, listing Personal and a team organization, with a Create organization button](https://laraue.com/static/images/blog/docs/laraue-boards/login-organization.jpg)

## Step 3 — Share the invite link

Open **Settings → Permissions** in the sidebar. The invite link is already there, generated when you created the organization — there's nothing to set up.

![The Invite people card with the invite link](https://laraue.com/static/images/blog/docs/laraue-boards/invite-link.jpg)

Copy the link and send it to your teammate in Telegram, or however you prefer. When they open it and accept, they join the organization.

## Step 4 — Configure their access

A new member starts with **no permissions at all** — they can't see anything in the organization until you grant access. Open **Settings → Permissions**, find them in the member list, and tap through to their permissions page to set up what they can do. See [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions) for the full breakdown.

## Managing the invite link

The invite link does not expire. If you need to invalidate it — for example if you shared it publicly by mistake — use **Create a new link**. This requires the *Manage members and permissions* administrative permission. A new link is created and the old one stops working; members who already joined are not affected.

## Related pages

- [Organizations overview](/blog/documentation/laraue-boards/concepts/organizations)
- [Permissions management](/blog/documentation/laraue-boards/working-in-a-team/permissions)
- [Member management](/blog/documentation/laraue-boards/working-in-a-team/member-management)