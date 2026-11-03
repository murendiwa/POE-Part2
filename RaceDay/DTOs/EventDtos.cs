using RaceDay.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class EventRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Province { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    [Required, AllowedValues("Road Run", "Trail Run", "Fun Run", "Cycling", "Triathlon")]
    public string EventType { get; set; } = "";

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public EventStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? EventType { get; set; }
    public decimal DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
}

public class EventDetailResponse : EventResponse
{
    public List<CategorySummary> Categories { get; set; } = new();
}

public class CategorySummary
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}