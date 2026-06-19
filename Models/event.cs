namespace ExperimentLab.Models;

// One thing that happened to one user in one experiment.
public class Event
{
    public int Id { get; set; }
    public int ExperimentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // "exposure" | "conversion"
    public double? Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}