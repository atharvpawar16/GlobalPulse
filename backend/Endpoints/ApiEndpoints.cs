using GlobalPulse.Api.Models;
using GlobalPulse.Api.Services;

namespace GlobalPulse.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Events API ────────────────────────────────────────────────────────────────
        app.MapGet("/api/events", async (
            DbService db, ILogger<Program> logger,
            string? category, string? country, int? severity,
            int hours = 24, int limit = 200) =>
        {
            hours = Math.Clamp(hours, 1, 24 * 30);
            limit = Math.Clamp(limit, 1, 1000);
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
            hours = Math.Clamp(hours, 1, 24 * 30);
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

        app.MapPost("/api/alerts", async (DbService db, AlertRule rule) =>
        {
            if (string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Length > 100)
                return Results.BadRequest("Name is required and must be under 100 characters.");
            rule.MinSeverity = Math.Clamp(rule.MinSeverity, 1, 5);
            var id = await db.InsertAlertRuleAsync(rule);
            return Results.Created($"/api/alerts/{id}", new { id });
        });

        app.MapDelete("/api/alerts/{id:long}", async (DbService db, long id) =>
        {
            return await db.DeleteAlertRuleAsync(id) ? Results.NoContent() : Results.NotFound();
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
            return await db.ToggleFeedAsync(id, active)
                ? Results.Ok(new { id, active })
                : Results.NotFound();
        });


        // ── Health ────────────────────────────────────────────────────────────────
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

        return app;
    }
}
