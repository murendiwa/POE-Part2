using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Event
{
    // Primary key (EventId used across code and tests)
    public int EventId { get; set; }

    // Backwards-compatible Id property (maps to EventId)
    public int Id { get => EventId; set => EventId = value; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public DateTime EventDate { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    public DateTime CreatedAt { get; set; }

    // Foreign key + navigation to the organiser (a User)
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public string? Province { get; set; }
    [MaxLength(500)] public string? BannerUrl { get; set; }
}