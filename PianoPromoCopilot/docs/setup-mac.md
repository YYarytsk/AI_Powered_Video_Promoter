# PianoPromoCopilot - macOS Setup Guide

## Prerequisites for MacBook Pro M4 Max

### 1. Install Homebrew (if not already installed)

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### 2. Install .NET SDK

```bash
brew install --cask dotnet-sdk
```

Or download directly from https://dot.net

Verify:
```bash
dotnet --version
# Expected: 9.x.x or 10.x.x
```

### 3. Install Node.js and Angular CLI

```bash
brew install node
npm install -g @angular/cli@17
```

Or use nvm for version management:
```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
nvm install 22
nvm use 22
npm install -g @angular/cli@17
```

Verify:
```bash
node --version  # Expected: 22.x.x
ng version      # Expected: Angular CLI 17.x.x
```

### 4. Install Docker Desktop (OPTIONAL)

**Not required for the Quick Start below.** Docker is only needed if you want to run
against SQL Server instead of the in-memory database - see
[Optional: Run Against SQL Server](#optional-run-against-sql-server).

Download from https://docs.docker.com/desktop/mac/

---

## Quick Start

**No Docker, no database, and no API keys are required.** `dotnet run` uses the `http`
launch profile, which sets `ASPNETCORE_ENVIRONMENT=Development`, and
`appsettings.Development.json` sets `Features:UseInMemoryDatabase=true`. The API creates
an EF Core in-memory database on startup and seeds it with sample piano videos.

### 1. Run the Backend API

```bash
cd src/PianoPromoCopilot.Api
dotnet run
```

The API will:
- Start on http://localhost:5000
- Create the in-memory database (no migrations are run - migrations only apply to SQL Server)
- Seed sample data (3 piano videos, analytics, etc.)
- Open Swagger at http://localhost:5000/swagger

### 2. Run the Angular Frontend

In a new terminal:
```bash
cd client/piano-promo-copilot-client
npm install
ng serve
```

Frontend available at http://localhost:4200

Open http://localhost:4200 and you'll see the three seeded piano videos ready to work with.

> In-memory data resets every time the API restarts. To keep data between runs, use SQL
> Server (below).

---

## Detailed Setup

### Run with Mock Mode (No API Keys Required)

The app runs in mock mode by default in `Development`
(`src/PianoPromoCopilot.Api/appsettings.Development.json`):
- `Features:UseInMemoryDatabase=true` → EF Core in-memory DB, no Docker/SQL Server needed
- `Features:UseMockYouTube=true` → uses sample piano video data
- `OpenAI:ApiKey=your-openai-api-key-here` → any key starting with `your-` selects the
  deterministic mock LLM
- All features work without real YouTube or OpenAI credentials

### Environment Configuration

`.env.example` documents every available key, but **the .NET app does not read a `.env`
file**. There is no `DotNetEnv`-style loader in `Program.cs`, so copying `.env.example` to
`.env` and putting a real `OpenAI__ApiKey` in it has no effect - the app silently stays in
mock mode.

`.env.example` is useful for two things:
- `docker-compose` reads `.env` automatically (e.g. `MSSQL_SA_PASSWORD`)
- it lists the exact `Foo__Bar` names to use as real process environment variables

The API itself reads configuration from `appsettings.json` / `appsettings.Development.json`
and from real environment variables. So use one of:

```bash
# Option A: edit src/PianoPromoCopilot.Api/appsettings.Development.json directly

# Option B: export real environment variables in the shell that runs the API
export OpenAI__ApiKey="sk-proj-your-real-key"
export Features__UseMockYouTube=false
cd src/PianoPromoCopilot.Api && dotnet run
```

### Enable Real OpenAI

1. Get an API key from https://platform.openai.com
2. Update `appsettings.json`:
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-proj-your-real-key",
       "Model": "gpt-4o-mini"
     }
   }
   ```
3. Restart the API
4. The optimizer will now use real ChatGPT responses

### Enable Real YouTube (Advanced)

See `docs/youtube-integration-notes.md` for full OAuth setup.

---

## Optional: Run Against SQL Server

Only needed if you want data to persist between API restarts. Requires Docker Desktop to
be installed and running.

### 1. Start SQL Server

```bash
cd PianoPromoCopilot
docker-compose up -d sqlserver
```

Wait for SQL Server to be ready (about 30 seconds):
```bash
docker-compose ps
# sqlserver should show "healthy"
```

### 2. Turn Off the In-Memory Database

In `src/PianoPromoCopilot.Api/appsettings.Development.json` set:

```json
"Features": {
  "UseInMemoryDatabase": false
}
```

The API then uses `ConnectionStrings:DefaultConnection` and applies EF Core migrations on
startup before seeding.

### 3. Apply Migrations Manually (alternative to startup migration)

```bash
# From solution root
export PATH=$PATH:$HOME/.dotnet/tools
dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/PianoPromoCopilot.Infrastructure \
  --startup-project src/PianoPromoCopilot.Api
```

### View SQL Server Data

Connect with Azure Data Studio or any SQL client:
- Server: `localhost,1433`
- Authentication: SQL Login
- Username: `sa`
- Password: `PianoPromo@2024`
- Trust certificate: Yes

### Reset Database

```bash
docker-compose down -v  # removes volume
docker-compose up -d sqlserver
# API will re-migrate and re-seed on next startup
```

---

## Verification Checklist

```bash
# 1. API running
curl http://localhost:5000/api/health
# {"status":"healthy",...} ✓

# 2. Sample data loaded
curl http://localhost:5000/api/videos | jq length
# 3 ✓

# 3. Optimizer working (mock mode)
curl -X POST http://localhost:5000/api/videos/optimize \
  -H "Content-Type: application/json" \
  -d '{"title": "Test Piano Piece"}' | jq .compliance
# {"riskLevel":"Low","issues":[],"isSafeToUse":true} ✓

# 4. Frontend running
open http://localhost:4200
# Dashboard loads with sample videos ✓
```

Nothing above needs Docker. `docker-compose ps` is only relevant if you opted into SQL
Server.

---

## Troubleshooting

### SQL Server Won't Start

Only applies when running against SQL Server. If you just want the app working, leave
`Features:UseInMemoryDatabase=true` and skip Docker entirely.

```bash
# Check Docker is running
docker info

# Check if port 1433 is already in use
lsof -i :1433

# Force recreate the container
docker-compose down -v
docker-compose up -d sqlserver
```

### "Cannot connect to SQL Server"

The API will log an error but continue running. Check:
1. `Features:UseInMemoryDatabase` is `false` on purpose (if not, set it back to `true`)
2. Docker Desktop is running
3. SQL Server container is healthy: `docker-compose ps`
4. Password matches in both docker-compose.yml and appsettings.json

### dotnet-ef Command Not Found

```bash
# Add .NET tools to PATH
echo 'export PATH=$PATH:$HOME/.dotnet/tools' >> ~/.zshrc
source ~/.zshrc

# Or use full path
~/.dotnet/tools/dotnet-ef --version
```

### Angular CORS Errors

The API allows `http://localhost:4200` by default.
If using a different port, update `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:4200", "http://localhost:YOUR_PORT")
```

### M4 Mac Architecture Issues

SQL Server 2022 Docker image supports ARM64 via Rosetta.
If you have issues, ensure Rosetta is enabled in Docker Desktop settings.

### API Port Already in Use

```bash
# Check what's using port 5000
lsof -i :5000

# Change API port in appsettings.json
{
  "Urls": "http://localhost:5001"
}
```

Then update the frontend. The browser calls the API **directly** using the absolute
`apiUrl` in `client/piano-promo-copilot-client/src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5001/api'
};
```

`proxy.conf.json` is wired into the `serve` target in `angular.json`, but it never sees
these requests - it only proxies same-origin paths like `/api/...`, and `apiUrl` is an
absolute URL. Editing `proxy.conf.json` alone changes nothing.

Because the call is cross-origin, also add the new origin to the CORS policy in
`src/PianoPromoCopilot.Api/Program.cs` if you change the *frontend* port
(`policy.WithOrigins("http://localhost:4200", "http://localhost:4201", "http://localhost:3000")`).

---

## Development Tips

### Hot Reload (API)

```bash
dotnet watch run --project src/PianoPromoCopilot.Api
```

### Hot Reload (Angular)

```bash
ng serve  # Already has live reload by default
```

### Run Tests

```bash
dotnet test
```
