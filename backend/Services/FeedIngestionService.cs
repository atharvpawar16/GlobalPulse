using System.Text.Json;
using CodeHollow.FeedReader;
using GlobalPulse.Api.Models;
using StackExchange.Redis;

namespace GlobalPulse.Api.Services;

public class FeedIngestionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeedIngestionService> _logger;
    private readonly int _intervalMinutes;
    private readonly IConnectionMultiplexer? _redis;

    public FeedIngestionService(IServiceScopeFactory scopeFactory,
        IConfiguration config, ILogger<FeedIngestionService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _redis = redis;
        _intervalMinutes = config.GetValue<int>("FeedIntervalMinutes", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // wait for DB to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        while (!ct.IsCancellationRequested)
        {
            try { await IngestAllFeedsAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Feed ingestion error"); }

            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), ct);
        }
    }

    private async Task IngestAllFeedsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<DbService>();
        var pub = _redis?.GetSubscriber();

        var feeds = new List<dynamic>();
        try { feeds = (await db.GetActiveFeedsAsync()).ToList(); }
        catch { _logger.LogWarning("Could not load feeds from DB"); return; }

        foreach (var feed in feeds)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                _logger.LogInformation("Fetching feed: {Name}", (string)feed.name);
                var result = await FeedReader.ReadAsync((string)feed.url);

                foreach (var item in result.Items.Take(20))
                {
                    var ev = new Event
                    {
                        Source     = (string)feed.name,
                        Category   = EventClassifier.ClassifyCategory((string)feed.category, item.Title ?? ""),
                        Title      = EventClassifier.Truncate(item.Title ?? "(no title)", 500),
                        Summary    = EventClassifier.Truncate(EventClassifier.StripHtml(item.Description ?? ""), 2000),
                        Url        = item.Link,
                        Severity   = EventClassifier.GuessSeverity(item.Title ?? ""),
                        OccurredAt = item.PublishingDate?.ToUniversalTime() ?? DateTime.UtcNow,
                    };

                    var country = EventClassifier.ExtractCountry(ev.Title + " " + ev.Summary);
                    if (country != null)
                    {
                        ev.Country = country;
                        var coords = await GeocodeAsync(country);
                        if (coords.HasValue) { ev.Lat = coords.Value.lat; ev.Lng = coords.Value.lng; }
                    }

                    long id = 0;
                    try { id = await db.InsertEventAsync(ev); }
                    catch { /* DB not available */ }
                    ev.Id = id;

                    if (pub != null && id > 0)
                        await pub.PublishAsync(RedisChannel.Literal("events:new"), JsonSerializer.Serialize(ev));
                }

                try { await db.UpdateLastFetchedAsync((long)feed.id); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch feed {Url}", (string)feed.url);
            }
        }
    }

    // ── Geocoding via Nominatim ───────────────────────────────────────────────
    // Cache capped at 500 entries to prevent unbounded memory growth
    private static readonly Dictionary<string, (double lat, double lng)?> _geocodeCache = new();
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    private static async Task<(double lat, double lng)?> GeocodeAsync(string query)
    {
        if (_geocodeCache.TryGetValue(query, out var cached)) return cached;
        if (_geocodeCache.Count >= 500) _geocodeCache.Clear(); // simple cap
        try
        {
            _http.DefaultRequestHeaders.UserAgent.TryParseAdd("GlobalPulse/1.0");
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            var json = await _http.GetStringAsync(url);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() > 0)
            {
                var first = arr[0];
                var result = (
                    lat: double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                    lng: double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture)
                );
                _geocodeCache[query] = result;
                await Task.Delay(200); // respect Nominatim rate limit
                return result;
            }
        }
        catch { /* ignore geocoding failures */ }
        _geocodeCache[query] = null;
        return null;
    }
}
