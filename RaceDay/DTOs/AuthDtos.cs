using RaceDay.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public class ProfileResponse
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }     // use DateTime? if your User model does
    public string? ProfilePictureUrl { get; set; }
    public UserRole Role { get; set; }
}

public class ProfileUpdateRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(20)] public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}