# 🎹 PianoPromoCopilot

A YouTube promotion copilot for independent pianists publishing original compositions.

---

## ⚠️ Compliance Declaration

**This tool is designed exclusively for organic, compliant growth.**

PianoPromoCopilot does NOT and will NOT:
- ❌ Generate fake view/like/subscriber automation
- ❌ Build comment bots or engagement bots
- ❌ Create spam DM automation
- ❌ Suggest sub-for-sub schemes
- ❌ Mass post to unrelated communities
- ❌ Generate misleading titles or thumbnails
- ❌ Automate any posting without human review

Every generated suggestion requires **human review and manual action** before use.

---

## What It Does

PianoPromoCopilot helps an independent pianist:

- 🤖 **AI Optimization** - Generate SEO-optimized titles, descriptions, tags, and hashtags
- 📱 **Social Media Drafts** - Ready-to-review posts for Instagram, TikTok, Facebook, Reddit, X, LinkedIn, Email
- 🎬 **Shorts Ideas** - Suggestions for compelling 60-second clips
- 🖼️ **Thumbnail Concepts** - Visual ideas that convey the music's mood
- 📊 **Analytics Insights** - Performance-based growth recommendations
- 🛡️ **Compliance Review** - Every suggestion checked for YouTube ToS compliance

---

## Architecture

```
PianoPromoCopilot/
├── docker-compose.yml          # SQL Server 2022
├── .env.example                # Environment template
├── docs/                       # Documentation
│   ├── architecture.md
│   ├── api-endpoints.md
│   ├── llm-prompts.md
│   ├── setup-mac.md
│   └── youtube-integration-notes.md
├── database/
│   └── schema-notes.md
├── src/
│   ├── PianoPromoCopilot.Domain/       # Entities, enums
│   ├── PianoPromoCopilot.Application/  # DTOs, interfaces, services
│   ├── PianoPromoCopilot.Infrastructure/ # EF Core, LLM, YouTube clients
│   └── PianoPromoCopilot.Api/          # Controllers, Swagger, Program.cs
├── tests/
│   └── PianoPromoCopilot.Tests/        # Unit tests (xUnit + Moq)
└── client/
    └── piano-promo-copilot-client/     # Angular 17 frontend
```

**Stack:**
- Backend: ASP.NET Core 9 Web API
- Frontend: Angular 17 (standalone components)
- Database: SQL Server 2022 (Docker)
- ORM: Entity Framework Core 9
- LLM: OpenAI-compatible (mock fallback)
- Logging: Serilog

---

## Prerequisites (macOS)

```bash
# Install .NET SDK
brew install --cask dotnet-sdk

# Install Node.js
brew install node

# Install Angular CLI
npm install -g @angular/cli@17

# Install Docker Desktop (from docker.com)
# Then launch Docker Desktop app
```

For full macOS setup details, see [docs/setup-mac.md](docs/setup-mac.md).

---

## Quick Start

**No Docker, no database, and no API keys are required.** In `Development` the app runs
against an in-memory database seeded with sample piano videos.

### 1. Run the Backend

```bash
cd src/PianoPromoCopilot.Api
dotnet run
```

Backend: http://localhost:5000  
Swagger: http://localhost:5000/swagger

### 2. Run the Frontend

In a second terminal:

```bash
cd client/piano-promo-copilot-client
npm install
npm start          # or: ng serve
```

Frontend: http://localhost:4200

Open http://localhost:4200 and you'll see three seeded piano videos ready to work with.

---

## The Core Workflow

1. **Videos** — see your videos (seeded samples in mock mode)
2. **Optimize** — generate titles, descriptions, tags, thumbnails, Shorts ideas and social posts
3. **Review Suggestions** — approve or reject each generated suggestion individually
4. **Promote** — generate per-platform promotion drafts, edit them, approve them
5. **Mark Posted** — after *you* manually post, mark the draft as posted

Nothing is ever published automatically. Approving only records your review.

---

## Mock Mode (No API Keys Required)

The app works fully in mock mode — this is the default in `Development`:

| Feature | Mock Behavior |
|---------|--------------|
| Database | EF Core in-memory, seeded on startup (no Docker/SQL Server) |
| YouTube data | 3 sample piano videos with realistic metadata |
| LLM/AI | Deterministic high-quality templates (no OpenAI key) |
| Analytics | Seeded 8-day history per video |
| Optimization | Full response, compliance-checked |

Controlled by `src/PianoPromoCopilot.Api/appsettings.Development.json`:

```json
"Features": {
  "UseMockYouTube": true,
  "EnableYouTubeWriteActions": false,
  "UseMockAnalytics": true,
  "UseInMemoryDatabase": true
}
```

> In-memory data resets every time the API restarts. To keep data between runs, use
> SQL Server (below).

---

## Optional: Run Against SQL Server

```bash
# 1. Start SQL Server (wait ~30s for it to initialize)
docker-compose up -d sqlserver
docker-compose ps        # sqlserver should be "healthy"

# 2. Turn off the in-memory database
#    In appsettings.Development.json set:  "UseInMemoryDatabase": false

# 3. Run the API — it migrates and seeds automatically on startup
cd src/PianoPromoCopilot.Api && dotnet run
```

To apply migrations manually instead:

```bash
dotnet tool install --global dotnet-ef
export PATH=$PATH:$HOME/.dotnet/tools
dotnet ef database update \
  --project src/PianoPromoCopilot.Infrastructure \
  --startup-project src/PianoPromoCopilot.Api
```

---

## Enable Real OpenAI

1. Get an API key: https://platform.openai.com
2. Update `appsettings.json`:
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-proj-your-real-key",
       "Model": "gpt-4o-mini",
       "BaseUrl": "https://api.openai.com/v1/"
     }
   }
   ```
3. Restart the API

The app automatically switches from mock to real LLM when a valid key is present.

---

## Enable YouTube OAuth (Advanced)

See [docs/youtube-integration-notes.md](docs/youtube-integration-notes.md) for full setup.

Summary:
1. Create a Google Cloud project
2. Enable YouTube Data API v3
3. Create OAuth 2.0 credentials
4. Configure `Google__ClientId`, `Google__ClientSecret`
5. Set `Features__UseMockYouTube=false`
6. Implement the OAuth flow in `GoogleYouTubeService.cs`

**YouTube write actions are disabled by default** (`Features__EnableYouTubeWriteActions=false`).
Never enable write actions without full OAuth setup and testing.

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check |
| GET | `/api/videos` | List videos |
| GET | `/api/videos/{id}` | Get video |
| POST | `/api/videos` | Create video |
| PUT | `/api/videos/{id}` | Update video |
| POST | `/api/videos/optimize` | **AI optimize** |
| GET | `/api/videos/{id}/suggestions` | Get suggestions |
| POST | `/api/videos/{id}/suggestions/{sid}/approve` | Approve suggestion |
| POST | `/api/videos/{id}/suggestions/{sid}/reject` | Reject suggestion |
| GET | `/api/videos/{id}/promotion-drafts` | Get drafts |
| POST | `/api/videos/{id}/promotion-drafts` | Generate drafts |
| PUT | `/api/promotion-drafts/{id}` | Update draft |
| GET | `/api/videos/{id}/analytics` | Get analytics |
| POST | `/api/videos/{id}/analytics/mock` | Add mock analytics |
| POST | `/api/videos/{id}/recommendations` | Get recommendations |
| GET | `/api/youtube/channel` | Get channel |
| GET | `/api/youtube/videos` | Get YouTube videos |
| POST | `/api/youtube/sync` | Sync channel |

Full documentation with schemas: http://localhost:5000/swagger

---

## Run Tests

```bash
dotnet test
# 24 tests: Compliance, Analytics, Optimization services
```

---

## Troubleshooting

**SQL Server connection failed:**
```bash
docker-compose ps  # Check sqlserver is "healthy"
docker-compose logs sqlserver  # Check for errors
```

**Port 5000 in use:**
```bash
# Change in appsettings.json: "Urls": "http://localhost:5001"
# Update Angular proxy.conf.json target accordingly
```

**CORS errors in browser:**
Ensure the API is running before the frontend. Check `Program.cs` has the correct Angular dev port.

**`dotnet-ef` not found:**
```bash
export PATH=$PATH:$HOME/.dotnet/tools
```

**Mock mode not working:**
Check `Features:UseMockYouTube=true` in `appsettings.json`. Mock is the default.

---

## Next Steps for Production

1. **YouTube OAuth** - Implement `GoogleYouTubeService` OAuth flow
2. **Token encryption** - Encrypt/decrypt OAuth tokens at rest
3. **Authentication** - Add Google OAuth for app users
4. **Scheduled sync** - Add Hangfire/Quartz for periodic YouTube sync
5. **Analytics** - Connect real YouTube Analytics API
6. **Rate limiting** - Add API rate limiting
7. **Deployment** - Containerize API, deploy to Azure App Service or similar

---

## Contributing

This is an MVP codebase. Key extension points:
- `ILlmService` - Add other LLM providers (Anthropic, Ollama, etc.)
- `IYouTubeService` - Implement real Google OAuth
- `ComplianceReviewService` - Add more compliance rules
- `AnalyticsRecommendationService` - Add more heuristics

---

## License

MIT - See LICENSE file.
