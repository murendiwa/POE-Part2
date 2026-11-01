namespace RaceDay.Web.Models;

public class EventListViewModel
{
    public static readonly string[] Provinces =
    [
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    ];

    public List<EventDto> Events { get; set; } = [];
    public string? Search { get; set; }
    public string? Province { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public HashSet<int> EnrolledCategoryIds { get; set; } = [];
}