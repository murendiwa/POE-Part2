using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }      // 0 = new category
    public int EventId { get; set; }
    public string? EventName { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100, ErrorMessage = "The name must be 100 characters or fewer.")]
    [Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Allow at least 1 participant.")]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000, ErrorMessage = "The entry fee can't be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<CategorySummaryDto> Categories { get; set; } = [];
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public static readonly string[] EnrolmentStatuses = ["Registered", "Confirmed", "Cancelled"];
    public static readonly string[] PaymentStatuses = ["Pending", "Paid", "Refunded"];

    public EventDetailDto Event { get; set; } = null!;
    public List<EnrolmentDto> Enrolments { get; set; } = [];
    public string? StatusFilter { get; set; }
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = null!;
    public ResultDto? Result { get; set; }
}

public class ResultGroup
{
    public CategorySummaryDto Category { get; set; } = null!;
    public List<ResultRow> Rows { get; set; } = [];
}

public class ResultsPageViewModel
{
    public static readonly string[] Outcomes = ["Finished", "DNF", "DNS"];

    public EventDetailDto Event { get; set; } = null!;
    public List<ResultGroup> Groups { get; set; } = [];
}