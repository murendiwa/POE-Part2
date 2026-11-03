using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class DashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = [];
    public List<OrganiserEventDto> Upcoming { get; set; } = [];
    public int TotalEnrolments { get; set; }
}

public class EventFormViewModel
{
    public static readonly string[] EventTypes = ["Road Run", "Trail Run", "Fun Run", "Cycling", "Triathlon"];
    public static readonly string[] Statuses = ["Scheduled", "Cancelled", "Completed"];

    public int EventId { get; set; }   // 0 = creating a new event

    // Match the StringLength values to your Part 1 column sizes.
    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The name must be 150 characters or fewer.")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "The description must be 1000 characters or fewer.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the date and time.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date and time")]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(200)]
    public string Location { get; set; } = "";

    public string? Province { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "";

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    public string Status { get; set; } = "Scheduled";

    [Display(Name = "Event banner")]
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBannerUrl { get; set; }
}