# PianoPromoCopilot - Database Schema Notes

## Database: PianoPromoCopilot (SQL Server 2022)

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

Or run the API and it will auto-migrate on startup.

---

## Entity Relationship Overview

```
AppUser (1) ---< YouTubeChannel (1) ---< YouTubeVideo
                                              |
                                              |--< VideoOptimizationSuggestion
                                              |--< PromotionDraft
                                              |--< VideoAnalyticsSnapshot

ComplianceReview (standalone, references by SourceType/SourceId)
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
Audit log of compliance checks.
- `SourceType`: LlmSuggestion | PromotionDraft | MetadataUpdate
- `RiskLevel`: Low | Medium | High | Blocked
- `Blocked` status prevents use of the content

---

## Compliance Design

The compliance system is intentionally conservative:
- Any suggestion matching banned patterns is flagged
- `Blocked` risk prevents the content from being usable
- `High` risk requires explicit human override
- All content requires human review before publication

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
