# 🌍 GlobalPulse

**Real-time world intelligence dashboard.** Monitor global conflicts, natural disasters, cyber threats, and breaking news — all on a live satellite map.

[![CI](https://github.com/atharvpawar16/GlobalPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/atharvpawar16/GlobalPulse/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Stars](https://img.shields.io/github/stars/atharvpawar16/GlobalPulse?style=social)](https://github.com/atharvpawar16/GlobalPulse/stargazers)

---

## Overview

GlobalPulse aggregates 40+ live news and intelligence feeds from around the world, classifies events using keyword analysis, geocodes them to exact map coordinates, and displays everything on an interactive satellite map — updating every 5 minutes automatically.

No paywalls. No API keys required. Fully open source.

---

## Screenshots

> *Satellite view with live event markers, dark mode*

![GlobalPulse Dashboard](https://raw.githubusercontent.com/atharvpawar16/GlobalPulse/main/docs/screenshot.png)

---

## Features

- **🛰 Satellite Map** — Switch between Satellite, Hybrid, and Street views powered by Esri World Imagery
- **📡 40+ Global Feeds** — BBC, Reuters, Al Jazeera, USGS, GDACS, Bellingcat, CISA, UN News and more
- **⚡ Live Updates** — Events refresh every 5 minutes, SignalR push when Redis is available
- **🤖 Auto Classification** — Headlines auto-tagged as Conflict / Disaster / Cyber / Political / News
- **📍 Auto Geocoding** — Country names extracted from headlines and pinned on the map
- **🔍 Search & Filter** — Filter by category, severity (1–5), time range; full-text search
- **🔔 Custom Alerts** — Create rules that fire toast notifications when matching events arrive
- **🌙 Dark / Light Mode** — Persists across sessions via localStorage
- **📱 Mobile Responsive** — Full-screen map with slide-in sidebar on mobile
- **💾 Demo Mode** — Works fully without a database, loads sample events instantly

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9 · SignalR · Dapper |
| Frontend | HTML · CSS · Vanilla JS — zero build tools |
| Database | PostgreSQL (Neon.tech serverless) |
| Map | Leaflet.js · Esri World Imagery · MarkerCluster |
| Geocoding | Nominatim (OpenStreetMap) |
| Live Push | SignalR WebSockets · Redis Pub/Sub |
| CI | GitHub Actions |

---

## Data Sources

**News** — BBC World · Reuters · AP News · Al Jazeera · France 24 · Deutsche Welle · Sky News · The Guardian · NPR · CNN · NBC · ABC · Euronews · NHK · Times of India · South China Morning Post · Middle East Eye · Africa News

**Conflict & Security** — Bellingcat · War on the Rocks · Defense News · ACLED

**Disasters** — USGS Earthquakes M2.5+ & M4.5+ · NASA FIRMS Wildfires · ReliefWeb · GDACS · Floodlist

**Cyber** — Krebs on Security · The Hacker News · Bleeping Computer · CISA Advisories · Dark Reading

**Geopolitical** — Foreign Policy · The Diplomat · Council on Foreign Relations · Politico · UN News

---

## Architecture

```
Browser (HTML/CSS/JS + Leaflet)
        │
        │  REST API + SignalR WebSocket
        ▼
ASP.NET Core 9
  ├── FeedIngestionService   — fetches 40+ RSS feeds every 5 min
  ├── Geocoding              — Nominatim API (country → lat/lng)
  ├── SignalR Hub            — pushes new events to all clients
  └── REST API               — /api/events, /api/alerts, /api/feeds
        │
        ▼
PostgreSQL (Neon serverless)
  events · alert_rules · feed_sources
```

---

## Project Structure

```
GlobalPulse/
├── backend/
│   ├── wwwroot/                    # Frontend — no build step needed
│   │   ├── index.html
│   │   ├── style.css
│   │   └── app.js
│   ├── Services/
│   │   ├── FeedIngestionService.cs # RSS aggregator + geocoder
│   │   └── DbService.cs            # PostgreSQL queries via Dapper
│   ├── Hubs/
│   │   └── EventsHub.cs            # SignalR real-time hub
│   ├── Models/
│   │   ├── Event.cs
│   │   └── AlertRule.cs
│   ├── Program.cs                  # Minimal API routes
│   ├── appsettings.json
│   └── Dockerfile
├── infra/
│   └── db/init.sql
├── .github/
│   └── workflows/ci.yml
├── docker-compose.yml
├── CONTRIBUTING.md
└── LICENSE
```

---

## Getting Started

```bash
git clone https://github.com/atharvpawar16/GlobalPulse.git
cd GlobalPulse
dotnet run --project backend/GlobalPulse.Api.csproj
```

Open **http://localhost:5164**

The app runs in demo mode out of the box — no database or API keys required. To connect a real database, set the `ConnectionStrings__Postgres` environment variable to any PostgreSQL connection string.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and pull requests are welcome.

---

## License

[MIT](LICENSE) © 2026 [atharvpawar16](https://github.com/atharvpawar16)
