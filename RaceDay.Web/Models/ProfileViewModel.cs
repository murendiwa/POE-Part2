using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string? Email { get; set; }   // read-only

    [StringLength(20, ErrorMessage = "The phone number is too long.")]
    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }
}