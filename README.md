# 🌍 GlobalPulse

**Real-time world intelligence dashboard.** Monitor global conflicts, natural disasters, cyber threats, and breaking news — all on a live satellite map.

[![CI](https://github.com/atharvpawar16/GlobalPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/atharvpawar16/GlobalPulse/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Live Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://globalpulse-mdmw.onrender.com)
[![Stars](https://img.shields.io/github/stars/atharvpawar16/GlobalPulse?style=social)](https://github.com/atharvpawar16/GlobalPulse/stargazers)

## 🔴 Live Demo

**[https://globalpulse-mdmw.onrender.com](https://globalpulse-mdmw.onrender.com)**

> Free tier — may take ~50s to wake up on first visit.

---

## Overview

GlobalPulse aggregates 40+ live news and intelligence feeds from around the world, classifies events using keyword analysis, geocodes them to exact map coordinates, and displays everything on an interactive satellite map — updating every 5 minutes automatically.

No paywalls. No API keys required. Fully open source.

---

## Features

| Feature | Description |
|---------|-------------|
| 🛰 **Satellite Map** | Switch between Satellite, Hybrid, and Street views (Esri World Imagery) |
| 📡 **40+ Live Feeds** | BBC, Reuters, Al Jazeera, USGS, GDACS, Bellingcat, CISA, UN News and more |
| ⚡ **Auto Updates** | Events refresh every 5 minutes from real RSS feeds |
| 🤖 **Auto Classification** | Headlines auto-tagged as Conflict / Disaster / Cyber / Political / News |
| 📍 **Auto Geocoding** | Country names extracted from headlines, pinned on map via Nominatim |
| 🔍 **Search & Filter** | Filter by category, severity (1–5), time range; full-text search |
| 🔔 **Custom Alerts** | Create rules that fire notifications when matching events arrive |
| 🌙 **Dark / Light Mode** | Persists across sessions |
| 📱 **Mobile Responsive** | Full-screen map with slide-in sidebar on mobile |
| 💾 **Demo Mode** | Works fully without a database — loads sample events instantly |

---

## Screenshots

> *Satellite view · Dark mode · Live event markers*

![GlobalPulse Dashboard](https://raw.githubusercontent.com/atharvpawar16/GlobalPulse/main/docs/screenshot.png)

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9 · SignalR · Dapper |
| Frontend | HTML · CSS · Vanilla JS — zero build tools |
| Database | PostgreSQL (Neon serverless) |
| Map | Leaflet.js · Esri World Imagery · MarkerCluster |
| Geocoding | Nominatim (OpenStreetMap) |
| Live Push | SignalR WebSockets · Redis Pub/Sub |
| Hosting | Render.com |
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
  └── REST API               — /api/events · /api/alerts · /api/feeds
        │
        ▼
PostgreSQL (Neon serverless)
  events · alert_rules · feed_sources
```

---

## Quick Start

```bash
git clone https://github.com/atharvpawar16/GlobalPulse.git
cd GlobalPulse
dotnet run --project backend/GlobalPulse.Api.csproj
```

Open **http://localhost:5164**

Runs in demo mode out of the box — no database or API keys required.

To connect a real database, set the `ConnectionStrings__Postgres` environment variable to any PostgreSQL connection string (e.g. [Neon.tech](https://neon.tech) free tier).

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
│   └── Dockerfile
├── infra/
│   └── db/init.sql                 # Database schema
├── .github/
│   ├── workflows/ci.yml            # GitHub Actions CI
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
├── CONTRIBUTING.md
├── SECURITY.md
├── CODE_OF_CONDUCT.md
└── LICENSE
```

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) — adding new feeds, improving classification, or fixing bugs are all great starting points.

## Security

See [SECURITY.md](SECURITY.md) for reporting vulnerabilities.

## License

[MIT](LICENSE) © 2026 [atharvpawar16](https://github.com/atharvpawar16)
