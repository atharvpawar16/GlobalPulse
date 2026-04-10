namespace GlobalPulse.Api.Models;

public class AlertRule
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Country { get; set; }
    public int MinSeverity { get; set; } = 1;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
