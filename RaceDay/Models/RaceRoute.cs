```csharp name=RaceDay/Models/RaceRoute.cs url=https://github.com/murendiwa/POE-Part2/blob/c2d1a6f4a067b6ec3f80b9f38be4c32d214d4652/RaceDay/Models/RaceRoute.cs
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class RaceRoute
{
    [Key]
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
