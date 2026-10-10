using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class User
{
    public int Id { get; set; }

    // Add your name fields here (FullName, FirstName/LastName, etc.) if your original had them.

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

    // Navigation properties
    public ICollection<Event> OrganisedEvents { get; set; } = [];

    public User(ICollection<Enrolment> enrolments)
    {
        Enrolments = enrolments;
    }

    public ICollection<Enrolment> Enrolments { get; set; } = [];
}