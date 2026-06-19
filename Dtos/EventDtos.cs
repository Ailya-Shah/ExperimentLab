using System.ComponentModel.DataAnnotations;

namespace ExperimentLab.Dtos;

public class RecordEventDto
{
    [Required] public int ExperimentId { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string Variant { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = string.Empty;
    public double? Value { get; set; }
}