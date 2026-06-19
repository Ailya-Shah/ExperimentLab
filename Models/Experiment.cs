using System.ComponentModel.DataAnnotations;

namespace ExperimentLab.Models;

/// <summary>
/// An A/B test. Has many Variants (e.g. "control" and "treatment").
/// </summary>
public class Experiment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // "draft" | "running" | "stopped" — kept as a simple string for Phase 1
    [MaxLength(20)]
    public string Status { get; set; } = "draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: EF Core uses this to link the related variants.
    public List<Variant> Variants { get; set; } = new();
}
