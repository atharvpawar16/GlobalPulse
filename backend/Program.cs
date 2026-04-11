using GlobalPulse.Api.Hubs;
using GlobalPulse.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Redis (optional — live push disabled if not available) ───────────────────
IConnectionMultiplexer? redis = null;
try
{
    redis = ConnectionMultiplexer.Connect(
        builder.Configuration["Redis"] ?? "localhost:6379");
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddHostedService<RedisEventRelay>();
    Console.WriteLine("✅ Redis connected — live push enabled");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Redis not available ({ex.Message}) — live push disabled (app still works)");
}

// ── Core services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<DbService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<FeedIngestionService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Init DB tables on startup (skipped gracefully if Postgres not running)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbService>();
    try
    {
        await db.InitAsync();
        Console.WriteLine("✅ Database ready");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  DB not available ({ex.Message}) — demo mode active");
    }
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();   // serves wwwroot/index.html

// ── Events API ────────────────────────────────────────────────────────────────
app.MapGet("/api/events", async (
    DbService db, ILogger<Program> logger,
    string? category, string? country, int? severity,
    int hours = 24, int limit = 200) =>
{
    try
    {
        var events = await db.GetEventsAsync(category, country, severity, hours, limit);
        return Results.Ok(events);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "GetEvents failed, returning empty");
        return Results.Ok(Array.Empty<object>());
    }
});

app.MapGet("/api/events/stats", async (DbService db, ILogger<Program> logger, int hours = 24) =>
{
    try
    {
        var stats = await db.GetStatsSummaryAsync(hours);
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "GetStats failed");
        return Results.Ok(Array.Empty<object>());
    }
});

// ── Alert Rules API ───────────────────────────────────────────────────────────
app.MapGet("/api/alerts", async (DbService db, ILogger<Program> logger) =>
{
    try { return Results.Ok(await db.GetAlertRulesAsync()); }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "GetAlerts failed");
        return Results.Ok(Array.Empty<object>());
    }
});

app.MapPost("/api/alerts", async (DbService db, GlobalPulse.Api.Models.AlertRule rule) =>
{
    if (string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Length > 100)
        return Results.BadRequest("Name is required and must be under 100 characters.");
    rule.MinSeverity = Math.Clamp(rule.MinSeverity, 1, 5);
    var id = await db.InsertAlertRuleAsync(rule);
    return Results.Created($"/api/alerts/{id}", new { id });
});

app.MapDelete("/api/alerts/{id:long}", async (DbService db, long id) =>
{
    await db.DeleteAlertRuleAsync(id);
    return Results.NoContent();
});

// ── Feed Sources API ──────────────────────────────────────────────────────────
app.MapGet("/api/feeds", async (DbService db, ILogger<Program> logger) =>
{
    try { return Results.Ok(await db.GetFeedSourcesAsync()); }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "GetFeeds failed");
        return Results.Ok(Array.Empty<object>());
    }
});

app.MapPatch("/api/feeds/{id:long}/toggle", async (DbService db, long id, bool active) =>
{
    await db.ToggleFeedAsync(id, active);
    return Results.Ok(new { id, active });
});

// ── Health ────────────────────────────────────────────────────────────────────
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

// ── SignalR Hub ───────────────────────────────────────────────────────────────
app.MapHub<EventsHub>("/hub/events");

app.Run();