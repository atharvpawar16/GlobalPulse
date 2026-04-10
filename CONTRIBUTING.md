# Contributing to GlobalPulse

Thank you for your interest in contributing! Here's everything you need to get started.

## Getting Started

**Requirements:** .NET 9 SDK only

```bash
git clone https://github.com/atharvpawar16/GlobalPulse.git
cd GlobalPulse
dotnet run --project backend/GlobalPulse.Api.csproj
```

Open http://localhost:5164 — the app runs in demo mode with no database needed.

## Project Structure

```
GlobalPulse/
├── backend/
│   ├── wwwroot/                 # Frontend — edit HTML/CSS/JS directly, no build step
│   ├── Services/
│   │   ├── FeedIngestionService.cs  # Add new data sources here
│   │   └── DbService.cs             # All database queries
│   ├── Hubs/EventsHub.cs        # SignalR real-time hub
│   ├── Models/                  # Event, AlertRule data models
│   └── Program.cs               # All API routes
├── infra/db/init.sql            # Database schema
└── .github/                     # CI, issue templates, PR template
```

## Ways to Contribute

### Add a new RSS feed source
Edit `backend/Services/DbService.cs` → find the `INSERT INTO feed_sources` block → add your feed:
```sql
('Source Name', 'https://example.com/feed.rss', 'rss', 'news'),
```
Categories: `news` · `conflict` · `disaster` · `cyber` · `political`

### Improve event classification
Edit `ClassifyCategory()` in `FeedIngestionService.cs` — add more keywords to improve accuracy.

### Improve the frontend
All frontend code is in `backend/wwwroot/` — plain HTML, CSS, JS. No build tools needed. Just edit and refresh.

### Fix a bug
Check the [Issues](https://github.com/atharvpawar16/GlobalPulse/issues) tab for open bugs.

## Pull Request Process

1. Fork the repo
2. Create a branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Verify it builds: `dotnet build backend/GlobalPulse.Api.csproj`
5. Commit with a clear message: `git commit -m "feat: add Reuters breaking news feed"`
6. Push and open a PR against `main`

## Commit Message Format

```
type: short description

Types: feat | fix | docs | refactor | chore
```

Examples:
- `feat: add GDELT geopolitical feed`
- `fix: correct geocoding for multi-word country names`
- `docs: update README with new data sources`

## Code Style

- **C#**: follow existing patterns, use `var` where type is obvious, async/await throughout
- **JS**: vanilla ES2020+, no frameworks, keep functions small and focused
- **CSS**: use CSS variables from `:root` for all colors — never hardcode hex values

## Questions?

Open a [Discussion](https://github.com/atharvpawar16/GlobalPulse/discussions) or email adp060606@gmail.com.
