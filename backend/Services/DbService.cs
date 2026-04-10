using Dapper;
using Npgsql;
using GlobalPulse.Api.Models;

namespace GlobalPulse.Api.Services;

public class DbService
{
    private readonly string _connStr;

    public DbService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("Postgres")!;
    }

    private NpgsqlConnection Conn()
    {
        // Neon.tech and other cloud Postgres providers require SSL
        var connStr = _connStr;
        if (!connStr.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
            !connStr.Contains("sslmode", StringComparison.OrdinalIgnoreCase) &&
            connStr.Contains("neon.tech"))
        {
            connStr += ";SSL Mode=Require;Trust Server Certificate=true";
        }
        return new NpgsqlConnection(connStr);
    }

    public async Task InitAsync()
    {
        using var db = Conn();
        await db.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS events (
                id          BIGSERIAL PRIMARY KEY,
                source      TEXT NOT NULL,
                category    TEXT NOT NULL,
                title       TEXT NOT NULL,
                summary     TEXT,
                url         TEXT UNIQUE,
                lat         DOUBLE PRECISION,
                lng         DOUBLE PRECISION,
                country     TEXT,
                region      TEXT,
                severity    SMALLINT DEFAULT 1,
                occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_events_url ON events (url) WHERE url IS NOT NULL;
            CREATE INDEX IF NOT EXISTS idx_events_category ON events (category);
            CREATE INDEX IF NOT EXISTS idx_events_occurred ON events (occurred_at DESC);
            CREATE TABLE IF NOT EXISTS alert_rules (
                id           BIGSERIAL PRIMARY KEY,
                name         TEXT NOT NULL,
                category     TEXT,
                country      TEXT,
                min_severity SMALLINT DEFAULT 1,
                active       BOOLEAN DEFAULT TRUE,
                created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            CREATE TABLE IF NOT EXISTS feed_sources (
                id           BIGSERIAL PRIMARY KEY,
                name         TEXT NOT NULL,
                url          TEXT NOT NULL UNIQUE,
                type         TEXT NOT NULL,
                category     TEXT,
                active       BOOLEAN DEFAULT TRUE,
                last_fetched TIMESTAMPTZ,
                created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            INSERT INTO feed_sources (name, url, type, category) VALUES
                -- Global wire services
                ('BBC World',           'https://feeds.bbci.co.uk/news/world/rss.xml',                                           'rss', 'news'),
                ('Reuters World',       'https://feeds.reuters.com/reuters/worldNews',                                           'rss', 'news'),
                ('AP News World',       'https://rsshub.app/apnews/topics/world-news',                                           'rss', 'news'),
                ('Al Jazeera',          'https://www.aljazeera.com/xml/rss/all.xml',                                             'rss', 'news'),
                ('France 24',           'https://www.france24.com/en/rss',                                                       'rss', 'news'),
                ('DW World',            'https://rss.dw.com/rdf/rss-en-world',                                                   'rss', 'news'),
                ('Sky News World',      'https://feeds.skynews.com/feeds/rss/world.xml',                                         'rss', 'news'),
                ('The Guardian World',  'https://www.theguardian.com/world/rss',                                                  'rss', 'news'),
                ('NPR World',           'https://feeds.npr.org/1004/rss.xml',                                                    'rss', 'news'),
                ('CNN World',           'http://rss.cnn.com/rss/edition_world.rss',                                              'rss', 'news'),
                ('NBC News World',      'https://feeds.nbcnews.com/nbcnews/public/world',                                        'rss', 'news'),
                ('ABC News International','https://abcnews.go.com/abcnews/internationalheadlines',                               'rss', 'news'),
                ('Euronews',            'https://www.euronews.com/rss',                                                          'rss', 'news'),
                ('NHK World',           'https://www3.nhk.or.jp/rss/news/cat0.xml',                                              'rss', 'news'),
                ('Times of India World','https://timesofindia.indiatimes.com/rssfeeds/296589292.cms',                            'rss', 'news'),
                ('South China Morning Post','https://www.scmp.com/rss/91/feed',                                                  'rss', 'news'),
                ('Middle East Eye',     'https://www.middleeasteye.net/rss',                                                     'rss', 'news'),
                ('Africa News',         'https://www.africanews.com/feed/rss',                                                   'rss', 'news'),
                ('Latin America Reports','https://www.reuters.com/rssFeed/latam',                                                'rss', 'news'),
                -- Conflict & security
                ('Bellingcat',          'https://www.bellingcat.com/feed/',                                                      'rss', 'conflict'),
                ('War on the Rocks',    'https://warontherocks.com/feed/',                                                       'rss', 'conflict'),
                ('Defense News',        'https://www.defensenews.com/arc/outboundfeeds/rss/',                                    'rss', 'conflict'),
                ('ACLED Blog',          'https://acleddata.com/feed/',                                                           'rss', 'conflict'),
                -- Disasters & environment
                ('USGS Earthquakes M2.5+','https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/2.5_day.atom',             'rss', 'disaster'),
                ('USGS Earthquakes M4.5+','https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/4.5_day.atom',             'rss', 'disaster'),
                ('NASA FIRMS Wildfires','https://firms.modaps.eosdis.nasa.gov/rss/firms_24hr.rss',                               'rss', 'disaster'),
                ('ReliefWeb Disasters', 'https://reliefweb.int/disasters/rss.xml',                                              'rss', 'disaster'),
                ('GDACS Alerts',        'https://www.gdacs.org/xml/rss.xml',                                                    'rss', 'disaster'),
                ('Floodlist',           'https://floodlist.com/feed',                                                           'rss', 'disaster'),
                -- Cyber & tech security
                ('Krebs on Security',   'https://krebsonsecurity.com/feed/',                                                     'rss', 'cyber'),
                ('The Hacker News',     'https://feeds.feedburner.com/TheHackersNews',                                          'rss', 'cyber'),
                ('Bleeping Computer',   'https://www.bleepingcomputer.com/feed/',                                                'rss', 'cyber'),
                ('CISA Alerts',         'https://www.cisa.gov/cybersecurity-advisories/all.xml',                                 'rss', 'cyber'),
                ('Dark Reading',        'https://www.darkreading.com/rss.xml',                                                   'rss', 'cyber'),
                -- Political & geopolitical
                ('Foreign Policy',      'https://foreignpolicy.com/feed/',                                                       'rss', 'political'),
                ('The Diplomat',        'https://thediplomat.com/feed/',                                                         'rss', 'political'),
                ('Council on Foreign Relations','https://www.cfr.org/rss/region/global',                                        'rss', 'political'),
                ('Politico World',      'https://rss.politico.com/politics-news.xml',                                           'rss', 'political'),
                ('UN News',             'https://news.un.org/feed/subscribe/en/news/all/rss.xml',                                'rss', 'political')
            ON CONFLICT (url) DO NOTHING;
        ");
    }

    // ── Events ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Event>> GetEventsAsync(
        string? category, string? country, int? minSeverity, int hours, int limit)
    {
        using var db = Conn();
        var sql = @"SELECT id, source, category, title, summary, url,
                           lat, lng, country, region, severity, occurred_at AS OccurredAt
                    FROM events
                    WHERE occurred_at >= NOW() - (@hours || ' hours')::interval
                      AND (@category IS NULL OR category = @category)
                      AND (@country  IS NULL OR country  = @country)
                      AND severity >= @minSeverity
                    ORDER BY occurred_at DESC
                    LIMIT @limit";
        return await db.QueryAsync<Event>(sql, new { hours, category, country, minSeverity = minSeverity ?? 1, limit });
    }

    public async Task<Event?> GetEventByIdAsync(long id)
    {
        using var db = Conn();
        return await db.QueryFirstOrDefaultAsync<Event>(
            "SELECT * FROM events WHERE id = @id", new { id });
    }

    public async Task<long> InsertEventAsync(Event e)
    {
        using var db = Conn();
        // Deduplicate by URL (if present) or title+source combo
        return await db.ExecuteScalarAsync<long>(@"
            INSERT INTO events (source, category, title, summary, url, lat, lng, country, region, severity, occurred_at)
            VALUES (@Source, @Category, @Title, @Summary, @Url, @Lat, @Lng, @Country, @Region, @Severity, @OccurredAt)
            ON CONFLICT DO NOTHING
            RETURNING id", e);
    }

    public async Task<bool> EventExistsAsync(string? url, string title, string source)
    {
        using var db = Conn();
        if (!string.IsNullOrEmpty(url))
            return await db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM events WHERE url = @url)", new { url });
        return await db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM events WHERE title = @title AND source = @source)", new { title, source });
    }

    public async Task<IEnumerable<dynamic>> GetStatsSummaryAsync(int hours)
    {
        using var db = Conn();
        return await db.QueryAsync(@"
            SELECT category, COUNT(*) AS count, MAX(severity) AS max_severity
            FROM events
            WHERE occurred_at >= NOW() - (@hours || ' hours')::interval
            GROUP BY category", new { hours });
    }

    // ── Alert Rules ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<AlertRule>> GetAlertRulesAsync()
    {
        using var db = Conn();
        return await db.QueryAsync<AlertRule>("SELECT * FROM alert_rules ORDER BY created_at DESC");
    }

    public async Task<long> InsertAlertRuleAsync(AlertRule r)
    {
        using var db = Conn();
        return await db.ExecuteScalarAsync<long>(@"
            INSERT INTO alert_rules (name, category, country, min_severity, active)
            VALUES (@Name, @Category, @Country, @MinSeverity, @Active)
            RETURNING id", r);
    }

    public async Task DeleteAlertRuleAsync(long id)
    {
        using var db = Conn();
        await db.ExecuteAsync("DELETE FROM alert_rules WHERE id = @id", new { id });
    }

    // ── Feed Sources ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<dynamic>> GetFeedSourcesAsync()
    {
        using var db = Conn();
        return await db.QueryAsync("SELECT * FROM feed_sources ORDER BY name");
    }

    public async Task ToggleFeedAsync(long id, bool active)
    {
        using var db = Conn();
        await db.ExecuteAsync("UPDATE feed_sources SET active = @active WHERE id = @id", new { id, active });
    }

    public async Task UpdateLastFetchedAsync(long id)
    {
        using var db = Conn();
        await db.ExecuteAsync("UPDATE feed_sources SET last_fetched = NOW() WHERE id = @id", new { id });
    }

    public async Task<IEnumerable<dynamic>> GetActiveFeedsAsync()
    {
        using var db = Conn();
        return await db.QueryAsync("SELECT * FROM feed_sources WHERE active = TRUE");
    }
}
