---
title: Building an AI-Powered Real Estate Ranking System with C#, Ollama, and a Custom Crawler
type: article
projects: [real-estate]
description: A technical deep-dive into an open-source real estate aggregator for Saint Petersburg — covering the C# / .NET 9 architecture, Ollama vision model integration, custom crawler design, and the ideality scoring formula.
createdAt: 2026-04-16
updatedAt: 2026-04-16
---
**[Laraue.Apps.RealEstate](https://github.com/win7user10/Laraue.Apps.RealEstate)** is an open-source proof-of-concept that crawls Saint Petersburg apartment listings, analyzes every listing photo with a local Ollama vision model, and ranks results by a composite ideality score. This article covers the architecture, each service's design, the ranking formula, and the evolution from self-trained TensorFlow models to open-source LLMs.

The live app runs at [apartments.laraue.com](https://apartments.laraue.com).

---

## Tech Stack

| Layer     | Technology                        |
|-----------|-----------------------------------|
| Language  | C#                                |
| Framework | .NET 9                            |
| API       | ASP.NET Core                      |
| Database  | PostgreSQL                        |
| Crawler   | Laraue.Crawler (in-house library) |
| Vision AI | Ollama — qwen2.5 vision model     |
| License   | MIT                               |

The system is split into three separate hosts, each with a focused responsibility.

---

## Architecture Overview

```
WorkerHost          → crawls listings + runs ranking calculations
GpuWorkerHost       → runs Ollama image prediction jobs
ApiHost             → serves frontend requests
```

Separating `GpuWorkerHost` from `WorkerHost` is the key architectural decision here. Image inference is GPU-bound and slow. Isolating it in its own host means the crawler and ranking jobs aren't blocked by prediction throughput, and the GPU host can be scaled or moved to a dedicated machine independently.

---

## Service 1: Advertisement Collector (WorkerHost)

The crawler runs every **4 hours** as a scheduled job inside `WorkerHost`. It's built on the in-house **[Laraue.Crawler](https://github.com/win7user10/Laraue.Crawling)** library — a .NET crawler with support for both static HTML (via AngleSharp) and JavaScript-rendered pages (via PuppeteerSharp).

Each source site is implemented as a `CrawlerJob` with a corresponding `CrawlingSchema`. For Cian (the primary Russian real estate aggregator), the relevant files are:

- [`CianCrawlerJob`](https://github.com/win7user10/Laraue.Apps.RealEstate/blob/main/src/Laraue.Apps.RealEstate.Crawling.Impl/Cian/CianCrawlerJob.cs) — the job that triggers the crawl
- [`CianCrawlingSchema`](https://github.com/win7user10/Laraue.Apps.RealEstate/blob/main/src/Laraue.Apps.RealEstate.Crawling.Impl/Cian/CianCrawlingSchema.cs) — the extraction schema mapping DOM elements to the data model

### Early Termination Logic

The crawler requests listings sorted by newest first. On each run, it inserts new records until it encounters one that already exists in the database — at which point it stops. This is a simple but effective deduplication and termination strategy: no need to crawl the full result set, just the delta since the last run.

Adding a new source site is a matter of implementing a new `CrawlerJob` + `CrawlingSchema` pair. The system supports as many sources as needed simultaneously.

---

## Service 2: Image Predictor (GpuWorkerHost)

The predictor runs every **minute** as a job inside `GpuWorkerHost`. It pulls the next batch of unanalyzed images and sends them one by one to Ollama.

### Prediction Model

The core class is [`OllamaRealEstatePredictor`](https://github.com/win7user10/Laraue.Apps.RealEstate/blob/main/src/Laraue.Apps.RealEstate.Prediction.Impl/OllamaRealEstatePredictor.cs), which calls a locally-hosted **qwen2.5** vision model. The image bytes are passed directly to Ollama along with a structured prompt that specifies what constitutes a good and a bad apartment photo.

### Prediction Result

Each photo produces an `OllamaPredictionResult`:

```csharp
public record OllamaPredictionResult
{
    public double RenovationRating { get; init; } // 0.0 to 1.0
    public string[] Tags { get; init; } = [];     // e.g. ["clean", "new_windows", "dark"]
    public string Description { get; init; } = string.Empty; // model's reasoning
}
```

Only `RenovationRating` feeds into the final ranking. `Tags` and `Description` are retained for prompt tuning and debugging — they let you see what the model is reacting to in each photo without having to re-run inference.

### Why Ollama Instead of a Cloud API

All inference runs locally. No images leave the machine, no per-call costs, and the model can be swapped by changing one config value. The `qwen2.5` vision model performs well enough for this task at reasonable speeds on consumer GPU hardware.

---

## Service 3: Ranking System (WorkerHost)

Once all photos for a listing have been predicted, the ranking job picks it up and computes the final **ideality score** via [`AdvertisementComputedFieldsCalculator`](https://github.com/win7user10/Laraue.Apps.RealEstate/blob/main/src/Laraue.Apps.RealEstate.Crawling.Impl/AdvertisementComputedFieldsCalculator.cs).

### Ideality Formula

The score starts at a maximum and accumulates **fines** for negative signals:

| Signal | Penalty |
|---|---|
| No nearby metro station | Fine applied |
| Metro station too far to walk | Fine applied |
| Far from city centre | Fine applied |
| Low renovation rating | Fine applied |

The more fines, the lower the ideality. This is intentionally a penalty-based system rather than a weighted sum — it's easier to reason about and tune, because each penalty has an isolated, interpretable effect.

### Renovation Rating

The renovation score for a listing is the **average `RenovationRating`** across all its photos. Listings with too few photos are excluded from the renovation ranking, since a single unrepresentative photo can skew the average significantly.

Once ideality and renovation rating are computed, the listing is promoted to the result set returned by the API.

---

## Service 4: API Host

Standard ASP.NET Core API. Supports filtering by rooms, price, district, and sorting by AI score or ideality. Nothing architecturally interesting here — the complexity lives in the other three services.

---

## Evolution: From TensorFlow to Ollama

The approach to image scoring changed significantly over the project's lifetime:

| Date | Approach |
|---|---|
| Feb 2023 | Dataset collection attempts; training with TensorFlow |
| Oct 2023 | First live version using **three custom trained models** (~22M parameters total). Fast inference, but poor accuracy — hard to collect enough correctly labelled training data |
| Sep 2025 | Self-trained models replaced with **Ollama + qwen2.5** |

The self-trained models were a dead end: collecting a large, correctly annotated dataset of apartment photos is genuinely difficult, and the models plateaued at accuracy levels that weren't useful for ranking. Switching to a pre-trained vision model via Ollama eliminated the dataset problem entirely and substantially improved prediction quality, at the cost of slower inference (offset by the dedicated `GpuWorkerHost`).

---

## Known Limitations

The project is a proof of concept and used informally. Key limitations worth knowing:

- **Prediction errors are common** — photo scoring works well on average but individual predictions can be wrong, especially for ambiguous or unusual photos
- **Averaging mitigates errors** — the per-listing aggregation smooths out individual photo mispredictions reasonably well in practice
- **Saint Petersburg data only** — the crawler schema is written for Cian's SPB listings; other cities would need separate schema implementations
- **Local GPU required** — running the `GpuWorkerHost` at useful throughput requires a local machine with a capable GPU

---

## Planned Articles

The README notes that several topics deserve dedicated writeups:

- How to select the right vision model for a photo-scoring task
- How to design and tune a penalty-based ranking formula
- How to integration-test a multi-service pipeline like this

---

## Contributing & Running Locally

The project is MIT-licensed. The most useful contributions would be new crawler schemas for additional real estate sites, or improvements to the Ollama prompt for better renovation scoring.

- **Repo:** [github.com/win7user10/Laraue.Apps.RealEstate](https://github.com/win7user10/Laraue.Apps.RealEstate)
- **Crawler library:** [github.com/win7user10/Laraue.Crawling](https://github.com/win7user10/Laraue.Crawling)
- **Live app:** [apartments.laraue.com](https://apartments.laraue.com)
