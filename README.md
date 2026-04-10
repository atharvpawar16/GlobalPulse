# 🌍 GlobalPulse

> Real-time world intelligence dashboard — satellite map, live crisis monitoring, 40+ global news feeds, AI-powered event classification.

[![CI](https://github.com/atharvpawar16/GlobalPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/atharvpawar16/GlobalPulse/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)
[![Deploy on Railway](https://railway.app/button.svg)](https://railway.app/new/template)

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🛰 **Satellite Map** | Esri World Imagery — switch between Satellite, Hybrid, Street |
| 📡 **40+ Live Feeds** | BBC, Reuters, Al Jazeera, USGS, GDACS, Bellingcat, CISA and more |
| ⚡ **Real-Time Push** | SignalR WebSockets — new events appear instantly (with Redis) |
| 🤖 **Auto Classification** | Events auto-tagged as Conflict / Disaster / Cyber / Political / News |
| 📍 **Auto Geocoding** | Country names extracted from headlines, pinned on map via Nominatim |
| 🔍 **Search & Filter** | Filter by category, severity, time range — search by keyword |
| 🔔 **Alert Rules** | Create custom rules — fires toast when matching event arrives |
| 🌙 **Dark / Light Mode** | Persists across sessions |
| 📱 **Mobile Responsive** | Hamburger sidebar, full-screen map on mobile |
| 💾 **Demo Mode** | Works fully offline with sample data — no DB needed |

---

## 🚀 Quick Start (Local)

**Requirements:** .NET 9 SDK only

```bash
git clone https://github.com/atharvpawar16/GlobalPulse.git
cd GlobalPulse
dotnet run --project backend/GlobalPulse.Api.csproj
```

Open **http://localhost:5164** — loads demo data instantly, no database needed.

### With a real database (Neon.tech — free)

1. Sign up at [neon.tech](https://neon.tech) → create a project → copy connection string
2. Edit `backend/appsettings.json`:
```json
"Postgres": "Host=...neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```
3. Restart — tables auto-create, feeds start pulling every 5 minutes

---

## 🌐 Deploy to Railway (free, 24/7)

1. Fork this repo
2. Go to [railway.app](https://railway.app) → New Project → Deploy from GitHub
3. Set root directory: `backend`
4. Add environment variable:
```
ConnectionStrings__Postgres=your_neon_connection_string
```
5. Deploy — Railway gives you a public URL

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Browser                          │
│  HTML + CSS + JS  ←→  Leaflet Map (Satellite)       │
│  SignalR Client   ←→  Live event push               │
└──────────────┬──────────────────────────────────────┘
               │ HTTP / WebSocket
┌──────────────▼──────────────────────────────────────┐
│              ASP.NET Core (.NET 9)                  │
│  REST API  │  SignalR Hub  │  Background Services   │
│                                                     │
│  FeedIngestionService  →  40+ RSS feeds every 5min  │
│  Geocoding (Nominatim) →  lat/lng from country name │
└──────────────┬──────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────┐
│         PostgreSQL (Neon.tech cloud)                │
│  events │ alert_rules │ feed_sources                │
└─────────────────────────────────────────────────────┘
```

---

## 📡 Data Sources

### News
BBC World · Reuters · AP News · Al Jazeera · France 24 · DW · Sky News · The Guardian · NPR · CNN · NBC · ABC · Euronews · NHK · Times of India · SCMP · Middle East Eye · Africa News

### Conflict & Security
Bellingcat · War on the Rocks · Defense News · ACLED

### Disasters
USGS Earthquakes (M2.5+ & M4.5+) · NASA FIRMS Wildfires · ReliefWeb · GDACS · Floodlist

### Cyber
Krebs on Security · The Hacker News · Bleeping Computer · CISA · Dark Reading

### Political
Foreign Policy · The Diplomat · CFR · Politico · UN News

---

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9, SignalR, Dapper |
| Frontend | Vanilla HTML/CSS/JS — no build tools |
| Database | PostgreSQL (Neon.tech) |
| Map | Leaflet.js + Esri World Imagery |
| Geocoding | Nominatim (OpenStreetMap) |
| CI/CD | GitHub Actions |
| Hosting | Railway.app |

---

## 📁 Project Structure

```
GlobalPulse/
├── backend/
│   ├── wwwroot/              # Frontend (index.html, style.css, app.js)
│   ├── Services/
│   │   ├── FeedIngestionService.cs   # RSS fetcher + geocoder
│   │   └── DbService.cs              # All DB queries
│   ├── Hubs/EventsHub.cs     # SignalR live push
│   ├── Models/               # Event, AlertRule
│   ├── Program.cs            # API routes
│   └── Dockerfile
├── infra/
│   └── db/init.sql
├── .github/workflows/ci.yml
├── docker-compose.yml
└── README.md
```

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). PRs welcome.

## 📄 License

[MIT](LICENSE) © 2026 atharvpawar16
