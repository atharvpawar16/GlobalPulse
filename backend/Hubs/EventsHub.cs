using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace GlobalPulse.Api.Hubs;

/// <summary>
/// SignalR hub — clients connect here to receive live event pushes.
/// The FeedIngestionService publishes to Redis "events:new";
/// this background listener forwards to all connected SignalR clients.
/// </summary>
public class EventsHub : Hub { }

public class RedisEventRelay : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<EventsHub> _hub;
    private readonly ILogger<RedisEventRelay> _logger;

    public RedisEventRelay(IConnectionMultiplexer redis,
        IHubContext<EventsHub> hub, ILogger<RedisEventRelay> logger)
    {
        _redis  = redis;
        _hub    = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var sub = _redis.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal("events:new"), async (_, value) =>
        {
            try { await _hub.Clients.All.SendAsync("NewEvent", value.ToString(), ct); }
            catch (Exception ex) { _logger.LogError(ex, "SignalR broadcast error"); }
        });

        await Task.Delay(Timeout.Infinite, ct);
    }
}
