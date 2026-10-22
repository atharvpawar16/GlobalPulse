using GlobalPulse.Api.Hubs;
using GlobalPulse.Api.Services;
using GlobalPulse.Api.Endpoints;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Redis (optional — live push disabled if not available) ───────────────────
var startupLog = new List<string>();
IConnectionMultiplexer? redis = null;
try
{
    redis = ConnectionMultiplexer.Connect(
        builder.Configuration["Redis"] ?? "localhost:6379");
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddHostedService<RedisEventRelay>();
    startupLog.Add("Redis connected, live push enabled");
}
catch (Exception ex)
{
    startupLog.Add($"Redis not available ({ex.Message}), live push disabled (app still works)");
}

// ── Core services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<DbService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<FeedIngestionService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();
foreach (var line in startupLog) app.Logger.LogInformation("{Message}", line);

// Init DB tables on startup (skipped gracefully if Postgres not running)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbService>();
    try
    {
        await db.InitAsync();
        app.Logger.LogInformation("Database ready");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("DB not available ({Message}), demo mode active", ex.Message);
    }
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();   // serves wwwroot/index.html

app.MapApiEndpoints();

// ── SignalR Hub ───────────────────────────────────────────────────────────────
app.MapHub<EventsHub>("/hub/events");

app.Run();