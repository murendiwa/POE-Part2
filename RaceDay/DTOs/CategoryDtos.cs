using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class CategoryRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}