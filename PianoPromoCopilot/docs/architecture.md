# PianoPromoCopilot - Architecture

## Overview

PianoPromoCopilot is a clean-architecture ASP.NET Core Web API with an Angular frontend.
It uses a feature-flag driven design that allows running fully in mock mode without any
external API credentials.

---

## Backend Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Angular Client                        │
│            (piano-promo-copilot-client)                 │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP REST
┌─────────────────────▼───────────────────────────────────┐
│                  PianoPromoCopilot.Api                   │
│  Controllers: Health, Videos, Suggestions, Analytics,   │
│               PromotionDrafts, YouTube                   │
│  Middleware: GlobalExceptionMiddleware                   │
│  Swagger: OpenAPI documentation                         │
└──────────┬────────────────────────────────────┬─────────┘
           │ Interfaces                          │
┌──────────▼──────────────┐   ┌─────────────────▼────────┐
│  PianoPromoCopilot.     │   │ PianoPromoCopilot.       │
│  Application            │   │ Infrastructure           │
│                         │   │                          │
│  Services:              │   │  AppDbContext (EF Core)  │
│  - VideoOptimization    │   │  Migrations              │
│  - ComplianceReview     │   │  Seed Data               │
│  - AnalyticsRecommend.  │   │                          │
│                         │   │  LLM:                    │
│  Interfaces:            │   │  - OpenAiLlmService      │
│  - ILlmService          │   │  - MockLlmService        │
│  - IYouTubeService      │   │                          │
│  - IComplianceReview    │   │  YouTube:                │
│  - IAnalytics...        │   │  - MockYouTubeService    │
│  - IAppDbContext        │   │  - GoogleYouTubeService  │
│                         │   │    (skeleton/TODO)       │
│  DTOs (request/resp)    │   │                          │
└──────────┬──────────────┘   └─────────────────┬────────┘
           │                                     │
┌──────────▼─────────────────────────────────────▼────────┐
│                PianoPromoCopilot.Domain                  │
│  Entities: AppUser, YouTubeChannel, YouTubeVideo,        │
│            VideoOptimizationSuggestion, PromotionDraft,  │
│            VideoAnalyticsSnapshot, ComplianceReview      │
│  Enums: SuggestionType, RiskLevel, PromotionStatus       │
└──────────────────────────────────────────────────────────┘
```

---

## Feature Flag Architecture

All external integrations are behind feature flags:

| Flag | Default | Description |
|------|---------|-------------|
| `Features:UseMockYouTube` | `true` | Use mock YouTube service |
| `Features:EnableYouTubeWriteActions` | `false` | Allow YouTube metadata updates |
| `Features:UseMockAnalytics` | `true` | Use mock analytics |
| `OpenAI:ApiKey` | empty | Use mock LLM when empty |

---

## Compliance Architecture

```
User Request → OptimizationService
                     ↓
              LLM generates JSON
                     ↓
              Parse & validate JSON
                     ↓
              ComplianceReviewService.ReviewAll()
              (checks all generated text)
                     ↓
              If Blocked/High → flag in response
                     ↓
              Save suggestions (with human review flags)
                     ↓
              Return response to user
```

**Key principle**: Every piece of AI-generated content is compliance-checked before
being returned to the user. The UI displays the compliance status prominently.
No content is automatically published - all requires human review and manual action.

---

## Data Flow: Video Optimization

```
POST /api/videos/optimize
{
  title: "Moonlit Reverie",
  compositionName: "Moonlit Reverie",
  mood: "peaceful",
  style: "impressionist",
  ...
}
    ↓
VideoOptimizationService
    ↓
ILlmService.GenerateAsync(systemPrompt, userPrompt)
    ↓  (OpenAI or Mock)
Parse JSON response
    ↓
IComplianceReviewService.ReviewAll()
    ↓
Save VideoOptimizationSuggestion records (if videoId provided)
    ↓
Return OptimizeVideoResponse
{
  titles: [...],
  descriptions: [...],
  tags: [...],
  hashtags: [...],
  thumbnailIdeas: [...],
  shortsIdeas: [...],
  socialPosts: {...},
  compliance: { riskLevel, issues, isSafeToUse }
}
```

---

## YouTube Integration Architecture

```
IYouTubeService (interface)
    ├── MockYouTubeService (default, no credentials needed)
    └── GoogleYouTubeService (TODO: implement OAuth flow)
            ├── Google OAuth 2.0 token refresh
            ├── YouTube Data API v3 - channels.list
            ├── YouTube Data API v3 - videos.list
            ├── YouTube Data API v3 - videos.update (WRITE - gated)
            └── YouTube Analytics API (future)
```

### Write Action Safety

`POST /api/youtube/videos/{id}/update-metadata` requires:
1. `Features:EnableYouTubeWriteActions=true` (disabled by default)
2. Compliance review passes
3. Human explicitly calls the endpoint (no automation)

---

## Angular Architecture

```
app.component (shell + nav)
    ├── dashboard (stats + recent videos)
    ├── video-list (table with actions)
    ├── video-detail (metadata + stats)
    ├── optimize-video (form + results)
    ├── promotion-drafts (platform cards)
    ├── analytics (snapshots + recommendations)
    └── settings (feature flags status)

Services (all injected via provideHttpClient):
    ├── ApiClientService (base HTTP)
    ├── VideoService
    ├── OptimizationService
    ├── PromotionDraftService
    ├── AnalyticsService
    └── YouTubeService
```

---

## Database Schema (simplified)

```sql
AppUser (Id, DisplayName, Email, CreatedAt)
    └── YouTubeChannel (Id, AppUserId, ChannelId, ChannelTitle, IsConnected, ...)
            └── YouTubeVideo (Id, YouTubeChannelId, YouTubeVideoId, Title, ...)
                    ├── VideoOptimizationSuggestion (Id, YouTubeVideoId, SuggestionType, SuggestionText, IsApproved, IsRejected)
                    ├── PromotionDraft (Id, YouTubeVideoId, Platform, DraftText, Status)
                    └── VideoAnalyticsSnapshot (Id, YouTubeVideoId, SnapshotDate, Views, Likes, ...)

ComplianceReview (Id, SourceType, SourceId, RiskLevel, Issues, Approved)
```

---

## Testing Architecture

```
PianoPromoCopilot.Tests (xUnit)
    ├── Services/ComplianceReviewServiceTests
    │   └── Tests all compliance rules and risk escalation
    ├── Services/AnalyticsRecommendationServiceTests
    │   └── Tests all recommendation heuristics
    └── Services/VideoOptimizationServiceTests
        └── Tests LLM parsing, fallback, compliance integration, DB persistence
```

Uses:
- xUnit for test framework
- Moq for mocking ILlmService
- FluentAssertions for readable assertions
- EF Core InMemory for DB tests

---

## Security Considerations

1. **No secrets in source code** - all credentials via env vars
2. **YouTube write actions gated** - require explicit feature flag
3. **Compliance reviews** - all AI content checked before returning to user
4. **Token encryption** - OAuth tokens stored encrypted in DB (TODO for real implementation)
5. **No automation** - every action requires human review and manual execution
6. **CORS** - only localhost Angular dev origins allowed in development
