using System.ComponentModel.DataAnnotations;

namespace ExperimentLab.Models;

/// <summary>
/// One arm of an experiment. TrafficPercentage is the share of users
/// assigned here. Exactly one variant per experiment is the control —
/// the baseline that every other arm is compared against.
/// </summary>
public class Variant
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Share of traffic for this variant, 0-100. The variants of one
    // experiment should sum to 100 (validated when we create them).
    [Range(0, 100)]
    public double TrafficPercentage { get; set; }

    // The baseline arm. Exactly one variant per experiment must be true.
    // Results compare every other variant against this one.
    public bool IsControl { get; set; }

    // Foreign key back to the parent experiment.
    public int ExperimentId { get; set; }
    public Experiment? Experiment { get; set; }
}