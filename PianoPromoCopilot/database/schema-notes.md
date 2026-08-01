# PianoPromoCopilot - Database Schema Notes

## Database: PianoPromoCopilot (SQL Server 2022)

> In `Development` the app defaults to `Features:UseInMemoryDatabase=true`, so none of the
> SQL Server steps below are needed to run it - the schema is created in memory with
> `EnsureCreatedAsync()` and no migrations run. Set the flag to `false` to use SQL Server.

### Running SQL Server

```bash
docker-compose up -d sqlserver
```

SQL Server will be available at `localhost:1433` with:
- Username: `sa`
- Password: `PianoPromo@2024`

### Applying Migrations

```bash
cd src/PianoPromoCopilot.Api
dotnet ef database update --project ../PianoPromoCopilot.Infrastructure
```

Or run the API with `Features:UseInMemoryDatabase=false` and it will migrate on startup.

---

## Entity Relationship Overview

```
AppUser (1) ---< YouTubeChannel (1) ---< YouTubeVideo
                                              |
                                              |--< VideoOptimizationSuggestion
                                              |--< PromotionDraft
                                              |--< VideoAnalyticsSnapshot

ComplianceReview (standalone, references by SourceType/SourceId - reserved, no rows written yet)
```

---

## Table Descriptions

### AppUser
Represents a user of the PianoPromoCopilot app.
- Used for future multi-user support
- Currently MVP uses a single implicit user

### YouTubeChannel
Represents a connected (or mock) YouTube channel.
- `IsConnected=false` → mock mode
- `AccessTokenEncrypted` / `RefreshTokenEncrypted` → for real OAuth (encrypted at rest)
- `TokenExpiresAt` → used to trigger token refresh

### YouTubeVideo
Core entity representing a YouTube video.
- `YouTubeVideoId` is the YouTube video ID (e.g., `dQw4w9WgXcQ`)
- `CompositionName`, `Mood`, `Style` → extra metadata for optimization prompts
- Statistics (`ViewCount`, `LikeCount`, `CommentCount`) are updated on sync

### VideoOptimizationSuggestion
LLM-generated suggestions for a video.
- `SuggestionType`: Title | Description | Tags | Hashtags | ThumbnailIdea | ShortsIdea | SocialPost | AnalyticsRecommendation
- `IsApproved` / `IsRejected` → human review tracking
- All suggestions require human review before use

### PromotionDraft
Social media post drafts for a video.
- `Status`: Draft | Approved | Rejected | PostedManually
- `PostedAt` → set when marked as PostedManually
- No automated posting - human must post manually

### VideoAnalyticsSnapshot
Daily analytics data for a video.
- `SnapshotDate` → one snapshot per day per video (or per traffic source/country breakdowns)
- `TrafficSource` examples: YT_SEARCH, EXT_URL, YT_CHANNEL, YT_BROWSE
- Used by AnalyticsRecommendationService to generate growth tips

### ComplianceReview
**Reserved for the planned compliance audit-log feature - not yet written by any code path.**
The table is mapped in `AppDbContext` and created by the initial migration, but no service
inserts rows into it today. Compliance verdicts are currently computed per request by
`ComplianceReviewService` and returned in the response (and as `X-Compliance-*` headers on
promotion draft generation) rather than persisted.

Planned shape:
- `SourceType`: LlmSuggestion | PromotionDraft | MetadataUpdate
- `RiskLevel`: Low | Medium | High | Blocked
- `Approved`: human review outcome

---

## Compliance Design

The compliance system is intentionally conservative. What it enforces today:

- Any generated text matching a banned pattern is flagged, and the highest matching risk
  level is returned with the response
- `Blocked` stops persistence and generation:
  - `VideoOptimizationService` returns the verdict but saves **no** `VideoOptimizationSuggestion`
    rows
  - `POST /api/videos/{id}/promotion-drafts` returns `422` and saves **no** `PromotionDraft` rows
- `POST /api/youtube/videos/{id}/update-metadata` refuses the write with `400` whenever
  `IsSafeToUse` is false - that is, for both `High` and `Blocked`. It is also gated behind
  `Features:EnableYouTubeWriteActions` (`403` when disabled, which is the default)
- `High` and `Medium` are otherwise **advisory signals**: the content is still saved and shown,
  flagged for the human to review and edit. There is no override/unlock mechanism in code -
  the human simply decides
- All content requires human review before publication; nothing is ever posted automatically

This ensures the system cannot be used for:
- Fake view/like/subscriber schemes
- Bot engagement
- Sub-for-sub manipulation
- Mass spam posting

---

## Seed Data

On first startup, the system seeds:
- 1 demo YouTube channel (mock)
- 3 sample piano videos with realistic metadata
- 8 days of analytics snapshots per video
- 2 sample optimization suggestions
- 1 sample promotion draft

This allows full testing without any external API connections.
