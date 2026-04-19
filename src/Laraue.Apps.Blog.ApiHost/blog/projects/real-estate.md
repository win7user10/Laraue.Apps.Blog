---
title: Laraue Apartments - Let AI Filter the Good Saint Petersburg Apartment Deals for You
type: project
projectType: application
githubLink: https://github.com/win7user10/Laraue.Apps.RealEstate
applicationLink: https://laraue.com/crawled-apartments
tags: [Crawler,AI,Ollama]
description: A free tool that crawls Saint Petersburg real estate listings and ranks them by renovation quality using AI photo analysis. Stop scrolling bad listings — see the best offers first.
createdAt: 2025-11-01
updatedAt: 2026-04-16
---
Scrolling through hundreds of apartment listings is exhausting. Half the photos are dark, blurry, or staged to hide problems. Prices vary wildly for seemingly similar flats. And unless you have years of market experience, it's hard to tell a great deal from a terrible one.

**[AI Apartments Aggregator](https://apartments.laraue.com)** is a free tool that automatically collects Saint Petersburg real estate listings and ranks them by renovation quality — using a local AI model that analyzes every photo. You see the best-looking apartments first, and skip past the ones not worth your time.

---

## The Problem with Apartment Hunting in Saint Petersburg

The Saint Petersburg real estate market is competitive. Whether you're looking to buy or rent, you'll face the same challenge: there are thousands of listings, and most platforms show them in an order that works for the seller, not for you.

Real estate professionals know what to look for — they can glance at a few photos and instantly estimate renovation quality, spot red flags, and gauge whether a price is fair for the district and condition. Regular buyers and renters don't have that experience, and making the wrong call can mean serious financial loss.

AI changes that. Photo analysis can now do what only experts could before: quickly evaluate the condition of an apartment from images and flag which listings are actually worth visiting.

---

## How It Works

The tool runs a four-step pipeline automatically, around the clock:

**1. Crawl listings** — The app scrapes real estate aggregators every 4 hours, collecting fresh Saint Petersburg apartment listings with all available metadata: price, district, number of rooms, size.

**2. Download photos** — For each listing, all photos are fetched and stored locally for analysis.

**3. AI photo scoring** — A local AI vision model (Ollama with qwen2.5) analyzes each photo and rates it from 0 to 10 based on renovation quality, cleanliness, and the overall condition visible in the image. The listing's final score is the average across all its photos.

**4. Browse and filter** — The ranked results appear in the web UI. Sort by AI score, filter by number of rooms, price, or district. The best-photographed, best-condition apartments appear at the top.

No cloud APIs. No data sent to third parties. Everything runs locally.

---

## What the AI Looks For

The model evaluates each photo for visual markers of quality:

- Renovation condition — new finishes vs. worn or deteriorated surfaces
- Cleanliness and maintenance
- Natural light and space
- Signs of damage, damp, or neglect

Each photo gets a score from 0 to 1, which is then aggregated across the full listing. Apartments with too few photos are excluded from ranking to reduce noise.

---

## The Ranking System

Photo quality alone doesn't make a good deal. The final **ideality score** combines multiple factors:

- **Renovation rating** — average AI score across all photos
- **Location** — proximity to a metro station and distance from the city centre apply penalties for bad location
- **Price** — factored into the overall value assessment

The further an apartment falls from a "perfect" benchmark on any of these axes, the more penalties it accumulates and the lower its ideality score. This lets you sort listings not just by cheapness or newness, but by genuine overall value.

---

## Filters Available

- Number of rooms (1-room, 2-room, etc.)
- Price range
- District / area of Saint Petersburg
- AI score threshold — show only listings above a quality floor

---

## Who This Is For

**Apartment hunters in Saint Petersburg** who are tired of manually sifting through low-quality listings and want to focus their time on viewings that are actually worth it.

**Investors** looking for underpriced properties in good condition relative to their asking price.

**Renters** who want to quickly compare renovation quality across listings at a similar price point.

---

## Try It

The app is a working prototype — real data, real AI scores, updated regularly from live Saint Petersburg listings.

**[Open the app at apartments.laraue.com](https://apartments.laraue.com)**