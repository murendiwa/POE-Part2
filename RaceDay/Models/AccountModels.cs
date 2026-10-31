using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose how you'll use RaceDay.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";   // "Participant" or "Organiser"
}

public record UserDto(int Id, string FullName, string Email, string Role, string? ProfilePictureUrl);