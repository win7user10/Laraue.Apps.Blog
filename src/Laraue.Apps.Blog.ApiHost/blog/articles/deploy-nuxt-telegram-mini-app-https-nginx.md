---
title: Serving a Nuxt Telegram Mini App over HTTPS — nginx, Let's Encrypt, and BotFather
description: Part 7 of building a Telegram task tracker solo. The production setup tutorials skip — serving a Nuxt Mini App over your own HTTPS with an nginx reverse proxy and Let's Encrypt, solving the certbot chicken-and-egg problem, registering it in BotFather, and testing locally with ngrok.
type: article
createdAt: 2026-06-22 17:00
updatedAt: 2026-06-22 17:00
projects: [boards]
tags: [nginx, nuxt, telegram-mini-app, https, lets-encrypt, certbot, ngrok, self-hosting, devlog]
previousLink: deploying-dotnet-postgres-vps-docker-compose
nextLink: telegram-mini-app-authentication-dotnet
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 7.
> The [previous article](deploying-dotnet-postgres-vps-docker-compose) put the bot on a server. The bot handles capture; this stage starts on the other half of the product — management — which lives in a web app.

The bot deliberately does one thing: it captures a message as a task. Everything else — organising those tasks on a board, moving them between columns, actually managing work — belongs in a web application. This article builds the first piece of that: not the app's features yet, but the foundation it has to stand on. The goal of this stage is narrow and concrete: get a Nuxt app to open inside Telegram as a Mini App.

The order of work is deliberate. First build the app, even an empty one, get it deploying to the server, and see the user object that Telegram provides come back. Then make it reachable over HTTPS — a subdomain, a reverse proxy, a certificate — because a Telegram Mini App has one hard prerequisite. And only at the very end, once there is a real HTTPS URL serving the app, tell Telegram about it.

## The app itself: an empty Nuxt project that knows it is in Telegram

The frontend began as an empty [Nuxt](https://nuxt.com/) application, scaffolded with Nuxt's own starter command, initialised in its own repository ([laraue-boards](https://github.com/win7user10/laraue-boards)), and pushed up. Since the rest of the toolchain here uses pnpm, the project was created with the pnpm form of the official command:

```bash
pnpm create nuxt@latest laraue-boards
```

That produces a minimal, runnable Nuxt project — no features, no components, no board, just the framework and an `app.vue`. Everything in this article is built on top of that starting point.

The one thing the first version did need was a way to know *how* it had been opened. A Mini App is launched inside Telegram, where Telegram hands it identifying data through its SDK. The app has to read that data and turn it into a logged-in user. That happens in a Nuxt plugin, [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts), which runs on startup. In its first version it was about as small as it could be:

```ts
export default defineNuxtPlugin(async (nuxtApp) => {
  const { setIsAppInitialized, setIsInMiniApp } = useAppState();
  const { setUser } = useAuth();
  try {
    const WebApp = (await import('@twa-dev/sdk')).default;
    const isInMiniApp = WebApp.initData !== '';
    if (isInMiniApp) {
      setIsInMiniApp(true);
      setUser(WebApp.initData);
    } else {
      throw Error('Init data object is missing');
    }
  } catch (err) {
    const { setInitError } = useAppState();
    setInitError(err);
  } finally {
    setIsAppInitialized(true);
  }
});
```

The key line is `WebApp.initData !== ''`. The `@twa-dev/sdk` package exposes Telegram's Mini App data; when the app runs inside Telegram, `initData` is populated, and when it is opened in a plain browser it is empty. That single check is how the app decides whether it is a real Mini App launch. If it is, the plugin records that and sets the user straight from the init data; if not, it throws, and the error is captured so the screen can show what went wrong. Either way it marks the app initialised at the end so the UI knows it can render.

It is worth being honest about how minimal this first version was. It took the Telegram init data and set the user directly from it — with **no verification that the data was actually valid**. There were no checks that the init data was authentic and untampered, which a real implementation absolutely needs (Telegram signs the init data precisely so the backend can verify it); that validation came later, and is part of the next article. There were also no components at all. The entire `app.vue` template existed only to prove that the plugin had run and produced a user object:

```vue
<template>
  <div id="app">
    <div v-if="initError">
      {{ initError }}
    </div>
    <div v-else>
      {{ appState.user }}
    </div>
  </div>
</template>
```

That is the whole UI: if initialisation failed, print the error; otherwise print the user object. No board, no styling, no navigation. The point of this first version was not to look like anything — it was to confirm, end to end, that the app loads, the plugin runs, the Mini App context is detected, and the user data comes back from Telegram. Once that was visible on screen, the foundation was proven and real UI could be built on top of it. (The current [`app.vue`](https://github.com/win7user10/laraue-boards/blob/master/app/app.vue) does far more, but it grew from this.)

## Shipping the frontend

With an app in the repository, the next step is getting it onto the server. The frontend has its own CI pipeline, separate from the bot's, because it is a separate repository with a completely different build. Where the bot's pipeline builds .NET and copies a compiled host, this one builds the Nuxt app and copies the static output:

```yaml
name: Build Vue App
on:
  push:
    branches: [ "master" ]
  pull_request:
    branches: [ "master" ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Install pnpm
        uses: pnpm/action-setup@v4
        with:
          version: 10
      - name: Use Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20.x'
          cache: 'pnpm'
      - run: pnpm i
      - run: pnpm nuxt generate
      - name: Upload
        if: github.ref == 'refs/heads/master'
        uses: marcodallasanta/ssh-scp-deploy@v1.2.0
        with:
          host: ${{ secrets.SSH_HOST }}
          port: ${{ secrets.SSH_PORT }}
          user: ${{ secrets.SSH_USER }}
          password: ${{ secrets.SSH_PASSWORD }}
          local: .output/*
          remote: "/home/laraue/note-to-board-frontend"
          post_upload: echo "Uploaded"
```

It installs pnpm and Node, runs `pnpm nuxt generate`, and — only on `master` — copies the output to the server over SSH, using the same `ssh-scp-deploy` action the bot's pipeline used. The pattern is identical: build in CI, SCP the result to a folder under `/home/laraue/` — here `note-to-board-frontend`, the exact folder the nginx container will mount as its web root.

The important detail is `nuxt generate` rather than `nuxt build`. `generate` pre-renders the app to a fully static site — plain HTML, CSS, and JavaScript files with no server component. That is precisely what allows the app to be served as files behind nginx with no Node process running, and it is the build-time decision the whole static-hosting setup depends on. (When the frontend later moves to server-side rendering for public boards, this is one of the things that changes: `generate` becomes `build`, and the static upload becomes a running SSR host.)

At this point the built app is sitting on the server in a folder, but nothing is serving it to the outside world yet. That is the next job.

## The subdomain, and why HTTPS now becomes the whole job

The app is built and on the server, but nothing serves it to the outside world, and here is where the Telegram Mini App's one hard prerequisite takes over the rest of the article. Telegram will not load a Mini App over plain HTTP — the URL you register must be HTTPS, with a valid certificate. This holds everywhere, with no exceptions: not in production, and not on your laptop during development either (a problem solved later with a tunnel). The bot needed none of this, because long polling reaches outward and never receives an inbound connection. The web app is the opposite: it exists to be reached, which means it has to be reached securely. So from here on, the work is a chain — a subdomain, a reverse proxy, a certificate — and it has to be done in that order.

It starts with the subdomain. The app lives at its own subdomain, `msgboard.laraue.com`, separate from the blog on the apex domain. Keeping it on a subdomain rather than a path under the main site keeps concerns cleanly separated — the blog and the task tracker are different applications with different deployment lifecycles, and a subdomain boundary reflects that, while keeping the TLS and proxy configuration for each independent.

Creating the subdomain is done wherever the domain is managed — the DNS or hosting panel of the domain registrar. There, you add a single `A` record for `msgboard` pointing at the VPS's IP address, the same server already running the bot and the blog. Nothing exotic: one more `A` record aimed at the server.

The one thing to be patient about is propagation. A new DNS record does not take effect instantly — it can take anywhere from a few minutes to a while longer for the `A` record to spread and for `msgboard.laraue.com` to actually resolve to the server. This matters for the next steps, because **the certificate cannot be issued until the subdomain resolves**. Let's Encrypt verifies domain ownership by connecting to the domain over the public internet, so if DNS has not propagated yet, that verification simply fails — with an error that names the problem directly:

```
DNS problem: NXDOMAIN looking up A for msgboard.laraue.com
  - check that a DNS record exists for this domain;
DNS problem: NXDOMAIN looking up AAAA for msgboard.laraue.com
  - check that a DNS record exists for this domain
```

`NXDOMAIN` means the name does not resolve yet — either the record was just added and has not propagated, or it is wrong. The practical sequence is: add the `A` record, wait until the subdomain resolves to the server (a quick `ping` or DNS lookup confirms it), and only then attempt to obtain the certificate. Trying to issue the certificate before the record resolves just produces the error above.

## nginx as the reverse proxy

In front of the app sits nginx, running as another container in the same Docker Compose stack as the bot and the database. Its job is to terminate TLS — to be the thing that holds the certificate and speaks HTTPS to the outside world — and to serve the app behind it.

Here is the nginx service in Compose:

```yaml
nginx:
  ports:
    - "80:80"
    - "443:443"
  build:
    context: .
    dockerfile: "NginxDockerfile"
  networks:
    - dockerapi-dev
  volumes:
    - ./../letsencrypt:/etc/letsencrypt
    - ./../.well-known/acme-challenge:/.well-known/acme-challenge
    - ./note-to-board-frontend/public:/usr/share/nginx/html/note-to-board-frontend
  command: "/bin/sh -c 'while :; do sleep 6h & wait $${!}; nginx -s reload; done & nginx -g \"daemon off;\"'"
```

Two of these volumes are worth pausing on, because they shape how the whole frontend is deployed.

**The frontend's built files are mounted straight into nginx as a volume.** That `./note-to-board-frontend/public:/usr/share/nginx/html/note-to-board-frontend` line maps the folder of built static files on the host — the same folder the CI pipeline uploads to — directly into the place nginx serves from. The consequence is the nice part: when a new version of the frontend is uploaded to that folder, nginx serves it **immediately**. There is no `docker-compose build`, no `docker-compose up -d`, no container restart for a frontend change — the files change on disk and the next request gets the new ones. Compared to the bot, where a deploy means rebuilding and recreating the container, the frontend deploy is just replacing files. So the CI pipeline from the previous section is the entire deploy: push to `master`, the build uploads, and the new frontend is live with nothing further to do on the server.

**The `command` keeps nginx reloading on a timer.** The shell loop reloads nginx every six hours (`nginx -s reload`) while it runs in the foreground. That periodic reload is mainly there so nginx picks up renewed TLS certificates without anyone intervening — certbot refreshes the certificate files on its own schedule, and this reload makes nginx start using the new ones.

This static-files-in-a-volume setup works today specifically because the Boards frontend is static. Later in the project the plan is to move it to a server-rendered host — for the reasons given back in the [stack article](choosing-stack-for-solo-project): if boards ever become public, server-side rendering is what lets their content be indexed and turn into organic traffic. At that point this approach is replaced by proxying to a running Nuxt SSR process, the same way the blog already works on this server. But that need has not arrived, and until it does, serving static files is simpler, cheaper, and faster to deploy.

The subdomain needs two server blocks. The first listens on port 80 and does two jobs: it serves the ACME challenge that Let's Encrypt uses to verify domain ownership (more on that below), and it redirects everything else to HTTPS:

```nginx
server {
    listen 80;
    server_name msgboard.laraue.com;
    location /.well-known/acme-challenge/ {
        root /.well-known/acme-challenge;
    }
    location / {
        return 301 https://$host$request_uri;
    }
}
```

The second block is the real one — port 443, TLS, and serving the app:

```nginx
server {
    listen 443 ssl;
    server_name msgboard.laraue.com;

    ssl_certificate     /etc/letsencrypt/live/msgboard.laraue.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/msgboard.laraue.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    root /usr/share/nginx/html/note-to-board-frontend;
    index index.html;

    location /_nuxt {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    location / {
        try_files $uri $uri/ /index.html;

        add_header Cache-Control "no-store, must-revalidate";
        add_header Pragma "no-cache";
        add_header Expires 0;
    }

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|json)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }
}
```

A few things in this block are worth drawing out.

**`root` points at the mounted static files.** This is the other end of the volume from the Compose service above — nginx serves the frontend straight from `/usr/share/nginx/html/note-to-board-frontend`, the built files mounted in from the host. No application server, no Node process; just files on disk.

**The `try_files ... /index.html` line is the SPA fallback.** A single-page app handles its own routing in the browser, so when someone navigates to a client-side route, nginx must not return a 404 — it has to hand back `index.html` and let the app's router take over. That one line is what makes deep links and in-app navigation work.

**The caching is deliberately split.** Nuxt's build output under `/_nuxt`, and static assets like JS, CSS, and images, are fingerprinted — their filenames change when their contents change — so they are marked `immutable` and cached for a year. But `index.html` itself is marked `no-store, must-revalidate`, so the browser always fetches a fresh copy. This is the standard SPA caching pattern: cache the fingerprinted assets forever, never cache the entry point, and a deploy is picked up instantly without serving stale code.

(The real config also has `location` blocks that proxy `/api/...` routes to the web API. Those belong to the next article, where the web API is built, so they are left out here — at this stage there is no backend for the app to call yet.)

## TLS certificates with Let's Encrypt and certbot

The certificate itself comes from Let's Encrypt, a free, automated certificate authority, issued and renewed with certbot. Free is the operative word: in keeping with the rest of this project's infrastructure choices, there is no reason to pay for a certificate when an automated, widely-trusted, free one does exactly the same job.

certbot runs as its own container in the same Compose stack, and its entire job is to keep the certificates renewed:

```yaml
certbot:
  image: certbot/certbot:v1.7.0
  networks:
    - dockerapi-dev
  volumes:
    - ./../letsencrypt:/etc/letsencrypt
    - ./../.well-known/acme-challenge:/var/www/certbot
  entrypoint: "/bin/sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"
```

The two volumes are the whole story of how this connects to nginx. The first, `letsencrypt`, is where certbot writes the issued certificates — on the host this is `/home/letsencrypt`, and nginx reads from the same directory, which is why the `ssl_certificate` paths in the server blocks above point at `/etc/letsencrypt/live/msgboard.laraue.com/`. certbot writes there; nginx reads from there; they share the folder. The second volume, the `acme-challenge` directory, is the shared space Let's Encrypt uses to verify that we actually control the domain.

That verification is the ACME challenge the port-80 server block was set up for. To issue a certificate for `msgboard.laraue.com`, Let's Encrypt asks the server to prove it controls the domain by placing a specific file under `/.well-known/acme-challenge/` and fetching it over plain HTTP. That is exactly why the port-80 block serves that path directly instead of redirecting it to HTTPS — the challenge has to work before any certificate exists. Once the file is fetched and verified, the certificate is issued.

The `entrypoint` is what makes renewal hands-off. It is a tiny shell loop: run `certbot renew`, sleep twelve hours, and repeat, forever. Let's Encrypt certificates last ninety days, and `certbot renew` only actually renews a certificate when it is close to expiry, so running it twice a day is harmless and means a certificate is always refreshed well before it lapses. There is no cron job to configure on the host, no calendar reminder, no manual renewal step — the container loops on its own for as long as the stack is running.

One thing this loop does *not* do is obtain the certificate the first time. `certbot renew` only renews certificates that already exist; the very first issuance for a new domain has to be bootstrapped separately.

### The chicken-and-egg problem: nginx won't start without a certificate

The first issuance has a genuine ordering problem worth understanding, because it is easy to trip over.

The problem is this: the port-443 server block references certificate files with `ssl_certificate` and `ssl_certificate_key`. If those files do not exist, **nginx refuses to start** — it will not boot a TLS server pointing at a missing certificate. But the certificate cannot be obtained until nginx is running to serve the ACME challenge. nginx needs the certificate to start; the certificate needs nginx to be issued. Neither can go first.

The initial certificate is obtained with a one-off run of the widely-used [`init-letsencrypt.sh`](https://github.com/wmnnd/nginx-certbot/blob/master/init-letsencrypt.sh) script, run locally against the server, which breaks that deadlock in a specific order:

1. It creates a **dummy self-signed certificate** at the path nginx expects, so the cert files exist even though they are fake.
2. With those files present, nginx can now **start** — the port-80 block (serving the ACME challenge) and the port-443 block (with the dummy cert) both come up.
3. It then **deletes the dummy certificate and requests the real one** from Let's Encrypt, which reaches the running nginx over port 80, completes the ACME challenge, and issues a genuine certificate.
4. It **reloads nginx**, which now picks up the real certificate in place of the dummy.

That dummy-certificate step is the whole trick: it lets nginx start so that the real certificate can be obtained, after which the fake one is thrown away. (This is also why the subdomain has to resolve first — step 3 fails with the `NXDOMAIN` error from earlier if DNS has not propagated.) After this bootstrap, the renewal container takes over and the certificate never needs manual attention again. Set it up once; it renews itself indefinitely.

At this point the app is live: built, deployed to the server, served by nginx over HTTPS at `msgboard.laraue.com`, with a valid auto-renewing certificate. Opening that URL in a normal browser shows the boilerplate app. Everything is in place except the one step that makes it a Mini App.

## Testing the Mini App locally with ngrok

Before that final step, a word on local development, because the HTTPS requirement does not go away on your laptop. Telegram still refuses to open a Mini App from `http://localhost:3000`, and you cannot point it at a URL that only exists on your machine.

The answer is [ngrok](https://ngrok.com/), which creates a public HTTPS URL that tunnels to a port on your local machine. With it, the frontend running locally on port 3000 gets a public `https://...ngrok-free.app` address that Telegram is willing to load. The full setup is in the frontend repository's [README](https://github.com/win7user10/laraue-boards), but the shape of it is:

1. **Tunnel the local ports.** An `ngrok.yml` config exposes both the frontend (3000) and the backend (5200) as public HTTPS URLs, started with `ngrok start --all`.
2. **Point the bot at the tunnel.** In BotFather's Mini App settings, the Main App button is set to the ngrok frontend URL instead of the production subdomain — so opening the Mini App in your bot loads your laptop's running app.
3. **Let the backend accept the tunnel.** The ngrok frontend URL is added to the backend's CORS allow-list, and the frontend's API base address is pointed at the ngrok backend URL.

That is enough to exercise the entire real authentication flow — the actual Telegram Mini App handshake — against code running and breakpointed on your own machine.

There is also a shortcut for when you do not need to test auth itself. The app supports a preauthorized-user mode: set a test user token in the frontend's `.env`, and the app launches as a known user with no Telegram handshake at all. That is the fast path for everyday UI work; the ngrok route is for when the authentication flow itself is what you are testing. (One small but easy-to-hit detail surfaces here — the backend has to allow the ngrok origin in CORS, or its responses come back empty inside Telegram. That class of CORS-only-fails-inside-Telegram problem is exactly what the next article runs into in production.)

A handy trick from the same setup, for anyone who has tried to debug a blank Mini App screen: there is no visible browser console inside Telegram. Injecting the `eruda` script into the app gives you an on-screen console button inside the Mini App, which turns "it just shows an empty screen and I have no idea why" into something you can actually inspect.

## The final step: registering the Mini App with BotFather

Everything is now in place — the app is built, deployed, and served over HTTPS at `msgboard.laraue.com`. The last thing to do is tell Telegram that this URL is the bot's Mini App. That happens in BotFather, the same bot that issued the token back in the previous article.

Open a chat with [@BotFather](https://t.me/BotFather) and select your bot. In its **Mini Apps** section, Telegram offers a couple of ways to launch a Mini App: bound to the bot's **menu button** (the button beside the chat input), or as a **Main App**. Either way, the step that matters is giving Telegram the Mini App **URL** — `https://msgboard.laraue.com`. For Boards, the Mini App is bound to the menu button.

Once that is set, the bot gains a launch button. In the chats list, the chat with the bot now shows a button to open the app right next to it:

![The launch Mini App button shown next to the bot chat](https://laraue.com/static/images/blog/articles/laraue-boards/message-board-bot-launch-mini-app-button.jpg)

Pressing that button is the moment everything in this article comes together. The Nuxt app opens in a panel over the chat, served securely from the subdomain, the `auth.init.ts` plugin runs, reads the Telegram init data, and — if everything is wired correctly — the screen shows the user object that came back. If something went wrong along the way, it shows the init error instead. That bare output, the user object or an error, is exactly what the minimal first version was built to display: proof that a tap inside Telegram travels all the way through HTTPS, nginx, the static app, and the plugin, and produces a real Telegram user.

This is also why the URL had to exist and be HTTPS *before* this step: BotFather is registering a real, reachable address, and Telegram will load only an HTTPS one. Doing it last, after the app is deployed and the certificate is valid, is the only order that works.

## What we have at the end

At the end of this stage there is no real product feature — and that is expected. What exists is the foundation the entire frontend will be built on: an empty-but-deploying Nuxt app, its own CI pipeline pushing it to the server, an nginx reverse proxy terminating TLS, a free auto-renewing Let's Encrypt certificate, and a Mini App registration that makes all of it open inside Telegram with a single tap. The infrastructure that the bot never needed — HTTPS, a domain, a proxy — is now standing, and everything the web app gains from here on loads through it.

It is worth noticing how much of this stage was operations rather than application code. That is the nature of a web frontend that has to live inside Telegram: the hard prerequisite is not building the page, it is making the page reachable, securely, at a real address. With that done once, it does not have to be done again.

## What comes next

The Mini App opens, but it cannot yet do anything — it has no backend to talk to, and the first version does not even really check that the logged-in user is who they claim to be. The next article stands up the web API: a second host alongside the bot, the authentication that actually validates the Telegram Mini App data, and the first real data flowing from the server into the app.