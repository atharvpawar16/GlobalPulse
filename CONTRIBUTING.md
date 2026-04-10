# Contributing to GlobalPulse

## Getting Started

1. Fork the repo and clone it
2. Install [.NET 9 SDK](https://dotnet.microsoft.com/download)
3. Copy `.env.example` → `.env` and fill in your keys (optional — app works without them)
4. Run: `dotnet run --project backend/GlobalPulse.Api.csproj`
5. Open `http://localhost:5164`

## Project Structure

```
backend/
  wwwroot/        ← Frontend (HTML/CSS/JS — edit these directly)
  Services/       ← FeedIngestionService, DbService
  Hubs/           ← SignalR EventsHub
  Models/         ← Event, AlertRule
  Program.cs      ← All API routes
```

## Ways to Contribute

- Add new RSS feed sources to `DbService.cs` seed data
- Improve category/severity classification in `FeedIngestionService.cs`
- Add new data source connectors (GDELT, ACLED, etc.)
- Improve the frontend map or UI
- Fix bugs and open issues

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR
- Test that the app builds: `dotnet build backend/GlobalPulse.Api.csproj`
- No breaking changes to existing API endpoints without discussion

## Code Style

- C#: follow existing patterns, use `var` where type is obvious
- JS: vanilla ES2020+, no frameworks, keep it simple
- CSS: use CSS variables from `:root` for all colors
