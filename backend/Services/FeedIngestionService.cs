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
                        Category   = ClassifyCategory((string)feed.category, item.Title ?? ""),
                        Title      = (item.Title ?? "(no title)")[..Math.Min(item.Title?.Length ?? 14, 500)],
                        Summary    = StripHtml(item.Description ?? "")[..Math.Min(StripHtml(item.Description ?? "").Length, 2000)],
                        Url        = item.Link,
                        Severity   = GuessSeverity(item.Title ?? ""),
                        OccurredAt = item.PublishingDate?.ToUniversalTime() ?? DateTime.UtcNow,
                    };

                    var country = ExtractCountry(ev.Title + " " + ev.Summary);
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

    private static string ClassifyCategory(string feedCategory, string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("earthquake") || t.Contains("flood") || t.Contains("wildfire") ||
            t.Contains("hurricane") || t.Contains("tsunami") || feedCategory == "disaster")
            return "disaster";
        if (t.Contains("war") || t.Contains("attack") || t.Contains("missile") ||
            t.Contains("troops") || t.Contains("conflict") || t.Contains("killed"))
            return "conflict";
        if (t.Contains("cyber") || t.Contains("hack") || t.Contains("breach") || t.Contains("ransomware"))
            return "cyber";
        if (t.Contains("election") || t.Contains("sanction") || t.Contains("president") ||
            t.Contains("parliament") || t.Contains("treaty"))
            return "political";
        return "news";
    }

    private static int GuessSeverity(string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("catastrophic") || t.Contains("mass casualt") || t.Contains("nuclear"))
            return 5;
        if (t.Contains("killed") || t.Contains("dead") || t.Contains("major") || t.Contains("critical"))
            return 4;
        if (t.Contains("attack") || t.Contains("explosion") || t.Contains("crisis"))
            return 3;
        if (t.Contains("warning") || t.Contains("alert") || t.Contains("protest"))
            return 2;
        return 1;
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", "").Trim();
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

    // Simple country name extractor from text
    private static readonly string[] _countries = {
        "Afghanistan","Albania","Algeria","Angola","Argentina","Australia","Austria",
        "Bangladesh","Belarus","Belgium","Bolivia","Brazil","Bulgaria","Cambodia",
        "Canada","Chile","China","Colombia","Croatia","Cuba","Czech Republic",
        "Denmark","Egypt","Ethiopia","Finland","France","Germany","Ghana","Greece",
        "Hungary","India","Indonesia","Iran","Iraq","Ireland","Israel","Italy",
        "Japan","Jordan","Kazakhstan","Kenya","Lebanon","Libya","Malaysia","Mexico",
        "Morocco","Myanmar","Netherlands","Nigeria","North Korea","Norway","Pakistan",
        "Palestine","Peru","Philippines","Poland","Portugal","Romania","Russia",
        "Saudi Arabia","Serbia","Somalia","South Africa","South Korea","Spain",
        "Sudan","Sweden","Switzerland","Syria","Taiwan","Thailand","Turkey",
        "Ukraine","United Kingdom","United States","USA","UK","Venezuela","Vietnam",
        "Yemen","Zimbabwe"
    };

    private static string? ExtractCountry(string text)
    {
        foreach (var c in _countries)
            if (text.Contains(c, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
}
