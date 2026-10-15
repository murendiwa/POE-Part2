using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class User
{
    // Primary key (use UserId throughout the codebase and tests)
    public int UserId { get; set; }

    // Backwards-compatible Id property (maps to UserId)
    public int Id { get => UserId; set => UserId = value; }

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
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

    // Parameterless constructor for model binding and ease of use
    public User()
    {
        Enrolments = new List<Enrolment>();
        OrganisedEvents = new List<Event>();
        CreatedAt = DateTime.UtcNow;
    }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public string FullName { get; set; } = string.Empty;
}