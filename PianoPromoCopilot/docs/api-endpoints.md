# PianoPromoCopilot - API Endpoints

Base URL: `http://localhost:5000/api`
Swagger UI: `http://localhost:5000/swagger`

---

## Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Check API health status |

---

## Videos

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/videos` | List all videos from local DB |
| GET | `/api/videos/{youtubeVideoId}` | Get a single video |
| POST | `/api/videos` | Create a manual video record |
| PUT | `/api/videos/{youtubeVideoId}` | Update video metadata |
| POST | `/api/videos/optimize` | Generate AI optimization (LLM) |

### POST /api/videos/optimize

Request:
```json
{
  "youTubeVideoId": "optional-string",
  "title": "required-string",
  "description": "optional",
  "compositionName": "optional",
  "mood": "optional",
  "style": "optional",
  "tempo": "optional",
  "keySignature": "optional",
  "targetAudience": "optional",
  "storyBehindComposition": "optional",
  "currentTags": ["tag1", "tag2"],
  "videoUrl": "optional"
}
```

Response:
```json
{
  "titles": ["title1", "title2", ...],
  "descriptions": ["desc1", ...],
  "tags": ["tag1", ...],
  "hashtags": ["#tag", ...],
  "thumbnailIdeas": ["idea1", ...],
  "shortsIdeas": [{
    "title": "",
    "hook": "",
    "suggestedTimestamp": "",
    "description": "",
    "caption": ""
  }],
  "socialPosts": {
    "instagram": "",
    "tikTok": "",
    "facebook": "",
    "reddit": "",
    "x": "",
    "linkedIn": "",
    "emailNewsletter": ""
  },
  "compliance": {
    "riskLevel": "Low|Medium|High|Blocked",
    "issues": [],
    "isSafeToUse": true
  }
}
```

---

## Suggestions

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/videos/{youtubeVideoId}/suggestions` | List suggestions for a video |
| POST | `/api/videos/{youtubeVideoId}/suggestions/{id}/approve` | Approve a suggestion |
| POST | `/api/videos/{youtubeVideoId}/suggestions/{id}/reject` | Reject a suggestion |

---

## Promotion Drafts

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/videos/{youtubeVideoId}/promotion-drafts` | List drafts for a video |
| POST | `/api/videos/{youtubeVideoId}/promotion-drafts` | Generate LLM drafts for all platforms |
| PUT | `/api/promotion-drafts/{id}` | Update draft text or status |

### Status Values
- `Draft` - Initial state, needs review
- `Approved` - Human reviewed and approved
- `Rejected` - Not suitable
- `PostedManually` - Human posted to platform

---

## Analytics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/videos/{youtubeVideoId}/analytics` | Get analytics snapshots |
| POST | `/api/videos/{youtubeVideoId}/analytics/mock` | Create mock snapshot for testing |
| POST | `/api/videos/{youtubeVideoId}/recommendations` | Get AI recommendations from analytics |

---

## YouTube Integration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/youtube/channel` | Get channel (mock or real) |
| GET | `/api/youtube/videos` | Get videos (mock or real), stores in DB |
| POST | `/api/youtube/sync` | Sync channel + videos |
| POST | `/api/youtube/videos/{id}/update-metadata` | Update live YouTube metadata (gated) |

### YouTube Write Actions

`POST /api/youtube/videos/{id}/update-metadata` requires:
- `Features:EnableYouTubeWriteActions=true` in config
- Request passes compliance review
- Real YouTube OAuth configured

When `EnableYouTubeWriteActions=false` (default), returns `403 Forbidden`.

---

## Sample Requests (curl)

```bash
# Health check
curl http://localhost:5000/api/health

# Get all videos
curl http://localhost:5000/api/videos

# Optimize a video (no API key = uses mock LLM)
curl -X POST http://localhost:5000/api/videos/optimize \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Moonlit Reverie",
    "compositionName": "Moonlit Reverie",
    "mood": "peaceful",
    "style": "impressionist",
    "targetAudience": "classical music lovers"
  }'

# Get suggestions for a video
curl http://localhost:5000/api/videos/mock_video_001/suggestions

# Approve a suggestion
curl -X POST http://localhost:5000/api/videos/mock_video_001/suggestions/1/approve

# Generate promotion drafts
curl -X POST http://localhost:5000/api/videos/mock_video_001/promotion-drafts \
  -H "Content-Type: application/json" -d '{}'

# Get analytics
curl http://localhost:5000/api/videos/mock_video_001/analytics

# Add mock analytics snapshot
curl -X POST http://localhost:5000/api/videos/mock_video_001/analytics/mock \
  -H "Content-Type: application/json" -d '{}'

# Get recommendations
curl -X POST http://localhost:5000/api/videos/mock_video_001/recommendations \
  -H "Content-Type: application/json" -d '{}'

# Get mock YouTube channel
curl http://localhost:5000/api/youtube/channel
```
