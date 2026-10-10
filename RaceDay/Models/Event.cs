using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Event
{
    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Participant;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
