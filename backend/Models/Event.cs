namespace GlobalPulse.Api.Models;

public class Event
{
    public long Id { get; set; }
    public string Source { get; set; } = "";
    public string Category { get; set; } = "";   // conflict | disaster | political | cyber | other
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public string? Url { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public int Severity { get; set; } = 1;        // 1-5
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
