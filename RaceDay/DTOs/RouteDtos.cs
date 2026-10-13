using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class RouteRequest
{
    [MaxLength(150)]
    public string? RouteName { get; set; }

    [Required, MaxLength(200)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string EndPoint { get; set; } = string.Empty;

    [Range(0, 10000)]
    public int? ElevationGainM { get; set; }

    [MaxLength(255), Url]
    public string? GpxFileUrl { get; set; }

    public string? Description { get; set; }
}

public class RouteResponse
{
    public int RouteId { get; set; }
    public int CategoryId { get; set; }
    public string? RouteName { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int? ElevationGainM { get; set; }
    public string? GpxFileUrl { get; set; }
    public string? Description { get; set; }
}