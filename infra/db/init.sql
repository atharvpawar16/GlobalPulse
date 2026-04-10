-- GlobalPulse database init (plain PostgreSQL 16+)

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

CREATE UNIQUE INDEX IF NOT EXISTS idx_events_url      ON events (url) WHERE url IS NOT NULL;
CREATE INDEX        IF NOT EXISTS idx_events_category ON events (category);
CREATE INDEX        IF NOT EXISTS idx_events_occurred ON events (occurred_at DESC);

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
    ('BBC World',             'https://feeds.bbci.co.uk/news/world/rss.xml',                                          'rss', 'news'),
    ('Al Jazeera',            'https://www.aljazeera.com/xml/rss/all.xml',                                            'rss', 'news'),
    ('USGS Earthquakes M2.5+','https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/2.5_day.atom',               'rss', 'disaster'),
    ('NASA FIRMS Wildfires',  'https://firms.modaps.eosdis.nasa.gov/api/country/csv/FIRMS_RSS/VIIRS_SNPP_NRT/world/1','rss', 'disaster')
ON CONFLICT (url) DO NOTHING;
