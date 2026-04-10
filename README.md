# 🌍 GlobalPulse

> Real-time world intelligence dashboard — live crisis monitoring, geopolitical tracking, and global event visualization.

![status](https://img.shields.io/badge/status-active-brightgreen) ![license](https://img.shields.io/badge/license-MIT-blue)

## Stack

| Layer    | Tech |
|----------|------|
| Backend  | .NET 9 / ASP.NET Core + SignalR |
| Frontend | Pure HTML + CSS + JS (no build tools) |
| Database | PostgreSQL |
| Cache    | Redis (live event pub/sub) |
| Map      | Leaflet.js (OpenStreetMap) |

## Run locally (no Docker)

**Requirements:** .NET 9 SDK, PostgreSQL, Redis

```bash
# 1. Clone
git clone https://github.com/yourusername/globalpulse
cd globalpulse

# 2. Configure
# Edit backend/appsettings.json — set your Postgres + Redis connection strings

# 3. Run
dotnet run --project backend/GlobalPulse.Api.csproj
```

Open http://localhost:5164 — the frontend is served automatically.

> No Postgres/Redis? The app still works — it loads demo data on the map.

## Run with Docker

```bash
docker compose up -d
```

Open http://localhost:5000

## Features

- 🗺️ Interactive dark world map with live event markers
- ⚡ Real-time event push via SignalR WebSockets
- 📡 Auto-ingests RSS feeds (BBC, Al Jazeera, USGS earthquakes, NASA wildfires)
- 🎨 Color-coded by category and severity (1–5)
- 🔍 Filter by category, time range, severity
- 📋 Event detail panel with source link
- 🔔 Toast alerts for high-severity events
- 💾 Works offline with demo data (no backend needed)

## Project Structure

```
globalpulse/
├── backend/
│   ├── wwwroot/          # Frontend (index.html, style.css, app.js)
│   ├── Models/           # C# data models
│   ├── Services/         # DbService, FeedIngestionService
│   ├── Hubs/             # SignalR EventsHub + RedisEventRelay
│   ├── Program.cs        # All API routes + app config
│   └── Dockerfile
├── infra/
│   └── db/init.sql
└── docker-compose.yml
```

## License

MIT
