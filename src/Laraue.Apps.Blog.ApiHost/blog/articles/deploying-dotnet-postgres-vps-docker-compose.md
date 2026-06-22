---
title: Deploying a .NET app and PostgreSQL to a cheap VPS with Docker Compose
description: Part 6 of building a Telegram task tracker solo. Real single-VPS deployment with Docker Compose — self-hosted PostgreSQL tuned for 1 GB of RAM, locked down with pg_hba and a VPN, a CI pipeline that ships on push, and the slim-image healthcheck gotcha that silently fails.
type: article
createdAt: 2026-06-22 12:00
updatedAt: 2026-06-22 12:00
projects: [boards]
tags: [docker, docker-compose, postgres, vps, self-hosting, devops, dotnet, devlog]
previousLink: clean-dotnet-telegram-bot-architecture
nextLink: deploy-nuxt-telegram-mini-app-https-nginx
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 6.
> The [previous article](clean-dotnet-telegram-bot-architecture) ended with a working backend that had never left the developer's machine. This one puts it on a real server.

At the end of the last article the bot worked locally but was not deployed. This article gets it onto a server people can actually reach, using the cheapest infrastructure that does the job: a small VPS, Docker Compose, a self-hosted PostgreSQL tuned for the memory it has, and a CI pipeline that ships on every push to `main`.

## Where it runs: a small VPS, made slightly bigger

There is no Kubernetes cluster here, no managed container service, no cloud database. The whole thing runs on a single small VPS.

The starting point was a server that already existed. This blog runs on a VPS with 1 GB of RAM, and rather than provision anything new, the simplest move was to bump that same server up to 3 GB. That leaves roughly 1 GB for the application side and 1 GB for PostgreSQL, with headroom for the operating system and the blog. One server, a few dollars a month, now hosting both the blog and the task tracker.

This is a deliberate position, not a temporary embarrassment to be upgraded away as soon as possible. For a product that is not yet earning anything, spending money on managed infrastructure is spending ahead of the need.

### Why self-hosted PostgreSQL instead of a managed database

A managed database — RDS, Cloud SQL, a hosted Postgres provider — gives you automated backups, failover, point-in-time recovery, and someone else's pager when something breaks at 3 a.m. Those are real benefits, and one day they may be worth paying for. They are not worth paying for now.

Self-hosting PostgreSQL on the same VPS is dramatically cheaper — effectively free, since the server is already paid for. For a project without meaningful load, a Postgres container next to the application is more than enough. The moment the product is actively used, and the cost of losing data or the value of squeezing out maximum performance starts to matter, moving to a managed database or a dedicated database server is a sensible step. Until that moment, paying managed-database prices to protect a workload that barely exists is the wrong trade.

The same logic will apply to files when the time comes. When the bot eventually stores media previews, they will go on the VPS's own filesystem rather than object storage like S3. Cloud file providers are, under the hood, a filesystem with an API and a bill attached — and at this scale the bare filesystem on a cheap server does the same job. When the product earns enough to justify the durability and scale guarantees of object storage, that move can be made. Not before — and since there are no files to store yet, that part stays out of the deployment entirely for now.

The principle underneath all of this is the same one that chose a boring stack: do not spend money on the performance and stability guarantees of a product that does not yet bring in income. Buy those guarantees when there is something to protect.

## Getting a bot token from BotFather

Before any of the deployment matters, the bot needs an identity on Telegram, and that comes from BotFather — Telegram's official bot for creating and managing bots. The process is short: open a chat with [@BotFather](https://t.me/BotFather), send `/newbot`, and answer two prompts — a display name and a username ending in `bot`. BotFather replies with an API token, a string like `123456789:AAH...`. That token is the bot's password; anything holding it can act as the bot.

The token is what goes into the deployment as `Telegram__Token`. It is supplied to the container through configuration on the server, never committed to the repository — the value shown in the Compose file later is a placeholder. BotFather is also where the bot's other settings live: its description, its commands list, and later in the series the Mini App button that opens the web app. For now, all that is needed is the token.

## The Dockerfile

The bot is packaged as a Docker image, and the Dockerfile is about as small as a .NET Dockerfile gets:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY boards-telegram-host app
WORKDIR app
ENTRYPOINT ["dotnet", "Laraue.Apps.Boards.TelegramHost.dll"]
```

It starts from the .NET 10 ASP.NET runtime image, copies in the already-built host output, and sets the entry point. Notably, there is no build step inside the Dockerfile — no `dotnet restore` or `dotnet publish`. The build happens earlier, in CI, and only the compiled output is copied in. That keeps the image small and the container build fast, and it means the image does not need the full .NET SDK, only the runtime.

One more honest detail: the Dockerfile is named `StructuredMessagesTelegramHostDockerfile`. That is the old project name from before the rename described in the last article — proof that the project really did start life as `Laraue.Apps.StructuredMessages`, and that some traces of an old name survive in the corners where renaming them buys nothing.

### Why the healthcheck needs curl installed

The one line in the Dockerfile that is not obvious is the `apt-get install curl`. The .NET runtime images are minimal — since .NET 8 they ship without `curl` or `wget` — so the container has no HTTP client at all out of the box. That is fine for the application, which has its own, but it breaks the container healthcheck, which needs to make an HTTP request to the bot's `/_health` endpoint from inside the container. The first version of this setup used a `wget`-based healthcheck that silently never worked, because `wget` was not installed: the command failed with "not found" on every run, so the container was perpetually reported unhealthy even while the bot was perfectly fine. Installing `curl` is what makes the healthcheck in the Compose file below actually able to run. If you have ever had a Docker healthcheck stuck "unhealthy" against a slim base image, this is very often why.

## Docker Compose: the bot and the database together

Compose ties the two containers — the bot and PostgreSQL — into one deployment. The full file is below; it is worth reading because almost every line is a small, deliberate decision.

```yaml
version: '3.4'
networks:
  dockerapi-dev:
    driver: bridge
services:
  structuredmessagestelegramhost:
    build:
      context: .
      dockerfile: "StructuredMessagesTelegramHostDockerfile"
    expose:
      - "5006"
    ports:
      - "8086:5006"
    restart: always
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      Kestrel__EndPoints__Http__Url: "http://+:5006"
      Telegram__Token: "TokenHere"
      ConnectionStrings__Postgre: "User ID=PostgresUser;Password=PostgresPass;Host=postgres;Port=5432;Database=laraue_messages_board;Command Timeout=0;"
      Logging__LogLevel__Default: "Warning"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - dockerapi-dev
    deploy:
      resources:
        limits:
          memory: 256M
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://localhost:5006/_health || exit 1"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 10s

  postgres:
    image: postgres:18-alpine
    container_name: postgres_db
    restart: always
    environment:
      POSTGRES_USER: PostgresUser
      POSTGRES_PASSWORD: PostgresPass
    volumes:
      - /home/laraue/postgres_data:/var/lib/postgresql/data
      - ./postgres.conf:/etc/postgresql/postgresql.conf
      - ./pg_hba.conf:/etc/postgresql/pg_hba.conf
    networks:
      - dockerapi-dev
    expose:
      - "5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U PostgresUser -d laraue_messages_board"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    deploy:
      resources:
        limits:
          memory: 1024M
```

A few things in here are worth pointing out.

**The limits are memory-only, and that is deliberate on a single core.** Each service has a memory ceiling — 256 MB for the bot, 1 GB for Postgres — but no CPU limit. This is a budget VPS with a single core, and that constraint shapes the choice. Memory limits genuinely protect you: they stop one container from ballooning and triggering an out-of-memory kill that takes down its neighbour, and the split (1 GB to the database that caches data, 256 MB to a bot that barely uses any) reflects how differently the two use memory. CPU is a different story. With only one core to share, a fixed CPU split is more trouble than it is worth: cap each container at half a core and you guarantee that neither can ever use the core even when the other is completely idle — which, for a long-polling bot that sits idle almost all the time, means artificially throttling Postgres for no reason. And the moment a third container arrives — the web API does, later in the series — fixed fractions stop adding up cleanly anyway: three services each capped at half of one core oversubscribe it, and the caps no longer protect each other. So CPU is left unlimited, and the kernel's normal fair scheduling shares it across whatever is actually busy at any moment. On a single-core box with a handful of mostly-idle containers, that is both simpler and better behaved than hand-tuned CPU fractions.

**The healthcheck pays off the work from the last article.** The bot's healthcheck uses `curl` to call the `/_health` endpoint that went in on day one — the same endpoint, and the reason `curl` had to be installed in the Dockerfile above. Compose uses the result to know whether the container is actually healthy, restarting it if it stops responding. Postgres has its own `pg_isready` check, pointed at the real user and database, and the bot's `depends_on` waits for the database to be *healthy*, not merely started, before the bot boots — so the host never comes up trying to migrate a database that is not ready yet.

**Postgres data lives in a direct bind mount.** `/home/laraue/postgres_data` on the host maps straight to the database's data directory. The data sits on the VPS's own disk, plainly visible on the host filesystem — which is exactly the cheap, self-hosted setup the earlier section argued for.

**The connection string and token shown here are placeholders.** Real secrets are not committed; these stand in for values supplied on the server.

### A database tuned for exactly 1 GB

Self-hosting does mean doing a little of the work a managed provider would do for you — in particular, telling Postgres how much memory it is allowed to use. The default Postgres configuration assumes nothing about its host, so on a memory-constrained VPS it is worth tuning. The config is sized for the 1 GB this instance has:

```ini
shared_buffers = 256MB
effective_cache_size = 768MB
work_mem = 16MB
maintenance_work_mem = 64MB
max_connections = 75
wal_buffers = 16MB
min_wal_size = 1GB
max_wal_size = 2GB
checkpoint_completion_target = 0.9
random_page_cost = 1.1
effective_io_concurrency = 200
```

The two numbers that matter most are `shared_buffers` (256 MB, the memory Postgres uses for caching data pages) and `effective_cache_size` (768 MB, a hint about how much memory is available for disk caching overall). `random_page_cost = 1.1` tells the planner that random reads are cheap, which is true on the VPS's SSD. None of this is exotic — it is the standard "tune Postgres for the RAM you actually have" pass, and it is the kind of thing a managed database hides from you. Doing it yourself is part of the cost of self-hosting, and at this scale it is a small, one-time cost.

### Locking the database to one address

One more piece of self-hosting hygiene: the database should not be reachable by the whole internet. Postgres access is restricted through `pg_hba.conf` so that the only external address allowed to connect is a single trusted IP. Everything else is rejected:

```
# TYPE  DATABASE  USER  ADDRESS         METHOD
local   all       all                   trust
host    all       all   127.0.0.1/32    md5
host    all       all   ServerIp/32     md5
host    all       all   0.0.0.0/0       reject
```

Local socket connections are trusted, localhost uses password auth, one specific IP is allowed with password auth, and everything else is explicitly rejected. Postgres stops at the first matching rule, so the final reject line is belt-and-braces clarity rather than strict necessity.

The `ServerIp` allowed here is not a generic "anyone" address — it is the address of a self-hosted VPN running on the same server. I connect to that VPN from my local machine (using Amnezia with AmneziaWG), and when it is active my laptop appears to Postgres as that trusted IP. So I can open the database directly from a local tool like DataGrip when I need to inspect or fix something — but only over the VPN. With the VPN off, the database is reachable by nothing on the public internet. It is a small file, but it is the difference between a database exposed to the world and one that is only reachable through a private tunnel I control.

## Connecting to Telegram: long polling

The bot talks to Telegram by long polling — it asks Telegram for updates rather than having Telegram push them to a public webhook. This was mentioned in the last article as the reason the host needs no inbound access, and at deploy time it is what makes the whole setup so simple. There is no public HTTPS endpoint to register, no inbound port to open for Telegram, no certificate required just for the bot to function. The container reaches out, maintains the connection, and receives updates over it. The only port mapping that exists is for local and health purposes, not for the internet to reach the bot.

That simplicity is a real deployment benefit. A webhook bot needs a publicly reachable HTTPS URL before it can receive a single message, which drags TLS and a reverse proxy into the picture immediately. Long polling lets the bot ship and run with none of that. (TLS and a reverse proxy do arrive later in the series — but for the frontend, where they are genuinely needed, not for the bot.)

## Shipping it: the CI pipeline

Deployment is automated from the first deploy, not done by hand over SSH. The workflow file lives at `.github/workflows/` in the repository — GitHub automatically discovers and runs any YAML file placed there, with no registration step beyond committing it. The GitHub Actions pipeline builds the host and copies the output to the server, which is exactly the artifact the Dockerfile expects:

```yaml
name: .NET
on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.x.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Upload Telegram Host
        if: github.ref == 'refs/heads/main'
        uses: marcodallasanta/ssh-scp-deploy@v1.2.0
        with:
          host: ${{ secrets.SSH_HOST }}
          port: ${{ secrets.SSH_PORT }}
          user: ${{ secrets.SSH_USER }}
          password: ${{ secrets.SSH_PASSWORD }}
          local: src/Laraue.Apps.Boards.TelegramHost/bin/Release/net10.0/*
          remote: "/home/laraue/boards-telegram-host"
          post_upload: echo "Uploaded successfully"
```

It is deliberately simple. On a push to `main` it checks out, sets up .NET 10, restores, builds in Release, and — only on `main` — uploads the built host to `/home/laraue/boards-telegram-host` over SSH. That is the same path the Dockerfile copies from (`COPY boards-telegram-host app`), so CI delivers the artifact and Compose builds the image around it. Connection details are GitHub secrets, never committed — those `${{ secrets.* }}` values are configured per-repository under Settings → Secrets and variables → Actions (`https://github.com/<user>/<repo>/settings/secrets/actions`), so the SSH host, port, user, and password live in GitHub's encrypted store rather than anywhere in the codebase. There is nothing clever here, and that is the point: build, copy to the server, done. The heavier machinery — registries, image scanning, blue-green deploys — is exactly what this scale does not need.

## How it fits together on the server

It helps to see where everything lands. On the VPS, a single directory, `/home/laraue/`, is the home for everything across all my projects on that server. It holds, side by side:

- The uploaded artifact folders — `boards-telegram-host` is where CI drops the built bot; a `boards-webapi-host` folder sits alongside it for the web API that arrives later in the series, and so on for each deployable.
- The Dockerfiles for each app.
- The config files — `postgres.conf`, `pg_hba.conf`, and the like.
- The single `docker-compose.yml` that describes the whole stack.
- The bind-mounted data directories — `postgres_data` and `storage` — that the containers write into.

So the SCP target in the pipeline, the `COPY` path in the Dockerfile, and the bind-mount paths in Compose all point into this one directory. The pipeline's only job is to refresh the artifact folders; everything else needed to run the stack is already sitting there.

Deployment itself is manual, and intentionally so. After the pipeline has uploaded the new artifact, I connect to the VPS over SSH and run two commands:

```bash
docker-compose build
docker-compose up -d
```

The build picks up the freshly uploaded host output and bakes it into the image; `up -d` recreates the changed containers and leaves them running in the background. That is the entire deploy. There is no orchestration platform deciding when to roll out, no automated step on the server reacting to the upload — I push, the artifact lands, and when I am ready I run two commands to bring the new version up. For one person deploying one product, that manual final step is a feature, not a gap: it is simple, predictable, and there is nothing to debug when it is just two commands I run myself.

With this in place, the loop is complete: push to `main`, the host is built and shipped to the VPS, and a manual `docker-compose build && up -d` brings it live against a self-hosted database. The bot is running.

## What we have at the end

At this point the bot does the one thing the user path called for: you send it a message, and it becomes a task. Here is roughly what that looks like in use — a message captured in Telegram showing up as a card on the board:

![A message sent to the bot appearing as a card on the board](https://laraue.com/static/images/blog/articles/laraue-boards/message-bot-board-example.jpg)

This screenshot is from the current version of the product, not the exact state at this point in the build — the interface has moved on since then. But the core flow it shows is approximately the one that existed once this deployment was working: a message goes to the bot, and it appears as a task you can see on a board. That loop, end to end on real infrastructure, is what this stage delivers.

## Where this leaves us

The bot is deployed on a cheap VPS, running in Docker alongside a self-hosted, memory-tuned PostgreSQL, shipped automatically by CI, connected to Telegram by long polling, and locked down at the database level. It is small, cheap, and live — capturing messages as tasks for a real user, on infrastructure that costs a few dollars a month.

Everything here was chosen to match the scale of a product that has not yet proven itself: self-hosted over managed, a bind mount over object storage, SCP over a container registry, long polling over webhooks. None of it is what you would build for scale, and all of it is right for where the product actually is.

## What comes next

With the bot live and capturing tasks, the next stage is the part it deliberately does not handle: management. That means a frontend — and before any of it can open inside Telegram as a Mini App, it needs HTTPS, which means a reverse proxy, a subdomain, and certificates. The next article builds that frontend foundation.