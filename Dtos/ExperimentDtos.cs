using System.ComponentModel.DataAnnotations;

namespace ExperimentLab.Dtos;

// ---- What the client SENDS to create an experiment ----
public class CreateVariantDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public double TrafficPercentage { get; set; }

    // Mark exactly one variant as the control (the baseline).
    public bool IsControl { get; set; }
}

public class CreateExperimentDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Must contain at least two variants whose percentages sum to 100,
    // exactly one of which is marked IsControl.
    [MinLength(2)]
    public List<CreateVariantDto> Variants { get; set; } = new();
}

// ---- What the API RETURNS (we don't expose EF navigation loops) ----
public class VariantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double TrafficPercentage { get; set; }
    public bool IsControl { get; set; }
}

public class ExperimentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<VariantDto> Variants { get; set; } = new();
}