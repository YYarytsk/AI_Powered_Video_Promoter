# YouTube Integration Notes

## Current State

The YouTube integration is built as a service abstraction with two implementations:
1. **MockYouTubeService** - Returns realistic sample data, no credentials needed
2. **GoogleYouTubeService** - Skeleton implementation with TODOs for real OAuth

## Feature Flags

```json
{
  "Features": {
    "UseMockYouTube": true,        // false = use real YouTube API
    "EnableYouTubeWriteActions": false  // true = allow videos.update
  }
}
```

## Setting Up Real YouTube OAuth

### Step 1: Google Cloud Console

1. Go to https://console.cloud.google.com
2. Create a new project (or use existing)
3. Enable **YouTube Data API v3**
4. Enable **YouTube Analytics API** (for analytics features)
5. Go to **Credentials** → **Create Credentials** → **OAuth 2.0 Client ID**
6. Application type: **Web application**
7. Add authorized redirect URI: `http://localhost:5000/auth/google/callback`
8. Note your **Client ID** and **Client Secret**

### Step 2: Configure Credentials

In `appsettings.json` or environment variables:
```json
{
  "Google": {
    "ClientId": "123456789.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-...",
    "RedirectUri": "http://localhost:5000/auth/google/callback"
  },
  "Features": {
    "UseMockYouTube": false
  }
}
```

### Step 3: Implement OAuth Flow (TODO in GoogleYouTubeService)

The `GoogleYouTubeService` has clear TODOs for implementing:

1. **Authorization URL generation**: Redirect user to Google OAuth consent screen
2. **Token exchange**: Exchange authorization code for access + refresh tokens
3. **Token storage**: Encrypt and store tokens in `YouTubeChannel` entity
4. **Token refresh**: Automatically refresh expired access tokens
5. **API calls**: videos.list, channels.list, videos.update

### Step 4: API Scopes Required

```
https://www.googleapis.com/auth/youtube.readonly    (read videos/channel)
https://www.googleapis.com/auth/youtube              (update metadata - only if write enabled)
https://www.googleapis.com/auth/yt-analytics.readonly (analytics)
```

---

## YouTube Data API v3 Reference

### channels.list (get channel info)
```
GET https://www.googleapis.com/youtube/v3/channels
  ?part=snippet,statistics
  &mine=true
  &key={API_KEY}
Headers: Authorization: Bearer {access_token}
```

### videos.list (get video details)
```
GET https://www.googleapis.com/youtube/v3/videos
  ?part=snippet,statistics,contentDetails
  &id={videoId1,videoId2}
  &key={API_KEY}
```

### playlistItems.list (get uploads)
```
GET https://www.googleapis.com/youtube/v3/playlistItems
  ?part=snippet
  &playlistId={uploadsPlaylistId}
  &maxResults=50
  &key={API_KEY}
```
The uploads playlist ID is found in channel.contentDetails.relatedPlaylists.uploads

### videos.update (update metadata - WRITE ACTION)
```
PUT https://www.googleapis.com/youtube/v3/videos?part=snippet
Body: {
  "id": "videoId",
  "snippet": {
    "title": "new title",
    "description": "new description",
    "tags": ["tag1", "tag2"],
    "categoryId": "10"  // 10 = Music
  }
}
```

**IMPORTANT**: Must fetch existing metadata first and preserve fields not being updated.

---

## Safety Rules for Write Actions

1. **Feature flag required**: `Features:EnableYouTubeWriteActions=true`
2. **Compliance check required**: ComplianceReviewService must return non-Blocked
3. **Human approval required**: Suggestions must be approved before being sent
4. **Preserve existing data**: Only update fields explicitly provided
5. **Audit log**: Log every write action with before/after state

---

## YouTube Analytics API Reference

```
GET https://youtubeanalytics.googleapis.com/v2/reports
  ?ids=channel==MINE
  &startDate=2024-01-01
  &endDate=2024-12-31
  &metrics=views,likes,comments,subscribersGained,estimatedMinutesWatched
  &dimensions=day
  &filters=video=={videoId}
```

### Metrics Available
- `views`, `likes`, `dislikes`, `comments`
- `subscribersGained`, `subscribersLost`
- `estimatedMinutesWatched`, `averageViewDuration`, `averageViewPercentage`
- `impressions`, `impressionClickThroughRate`
- `cardClickRate`, `cardTeaserClickRate`

### Dimensions Available
- `day`, `month`, `7DayTotals`
- `country`, `continent`, `province`
- `trafficSource`, `deviceType`
- `gender`, `ageGroup`

---

## Troubleshooting

### "quotaExceeded" Error
YouTube Data API v3 has a daily quota of 10,000 units. Common costs:
- videos.list: 1 unit per video
- videos.update: 50 units per update
- channels.list: 1 unit

If you hit quota limits, reduce sync frequency or use mock mode.

### OAuth Token Expiry
Access tokens expire after 1 hour. The GoogleYouTubeService should:
1. Check token expiry before each API call
2. Use refresh token to get new access token
3. Update stored token in YouTubeChannel entity
4. Implement exponential backoff on auth failures

### Rate Limiting
YouTube API has rate limits beyond quota units. Implement:
- Exponential backoff for 429/500 errors
- Caching of channel/video data
- Batching video ID requests (max 50 per request)
