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

### 4. Install Docker Desktop

Download from https://docs.docker.com/desktop/mac/

After installing, ensure Docker Desktop is running before starting SQL Server.

---

## Quick Start

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

### 2. Run the Backend API

```bash
cd src/PianoPromoCopilot.Api
dotnet run
```

The API will:
- Start on http://localhost:5000
- Auto-run migrations
- Seed sample data (3 piano videos, analytics, etc.)
- Open Swagger at http://localhost:5000/swagger

### 3. Run the Angular Frontend

In a new terminal:
```bash
cd client/piano-promo-copilot-client
npm install
ng serve
```

Frontend available at http://localhost:4200

---

## Detailed Setup

### Environment Configuration

Copy the example env file:
```bash
cp .env.example .env
```

Edit `.env` with your values (or update `appsettings.json` directly).

### Apply Database Migrations Manually

```bash
# From solution root
export PATH=$PATH:$HOME/.dotnet/tools
dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/PianoPromoCopilot.Infrastructure \
  --startup-project src/PianoPromoCopilot.Api
```

### Run with Mock Mode (No API Keys Required)

The app runs in mock mode by default:
- `Features:UseMockYouTube=true` → uses sample piano video data
- `OpenAI:ApiKey=your-key-here` → uses deterministic mock LLM
- All features work without real YouTube or OpenAI credentials

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

## Verification Checklist

```bash
# 1. SQL Server running
docker-compose ps
# sqlserver: healthy ✓

# 2. API running
curl http://localhost:5000/api/health
# {"status":"healthy",...} ✓

# 3. Sample data loaded
curl http://localhost:5000/api/videos | jq length
# 3 ✓

# 4. Optimizer working (mock mode)
curl -X POST http://localhost:5000/api/videos/optimize \
  -H "Content-Type: application/json" \
  -d '{"title": "Test Piano Piece"}' | jq .compliance
# {"riskLevel":"Low","issues":[],"isSafeToUse":true} ✓

# 5. Frontend running
open http://localhost:4200
# Dashboard loads with sample videos ✓
```

---

## Troubleshooting

### SQL Server Won't Start

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
1. Docker Desktop is running
2. SQL Server container is healthy: `docker-compose ps`
3. Password matches in both docker-compose.yml and appsettings.json

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
# And update Angular proxy.conf.json target
```

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
# Expected: 24 tests passed
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
