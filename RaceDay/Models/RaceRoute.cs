using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class RaceRoute
{
    public int RouteId { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [MaxLength(150)]
    public string? RouteName { get; set; }

    [Required, MaxLength(200)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string EndPoint { get; set; } = string.Empty;

    public int? ElevationGainM { get; set; }

    [MaxLength(255)]
    public string? GpxFileUrl { get; set; }

    public string? Description { get; set; }
}