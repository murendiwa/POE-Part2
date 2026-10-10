using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Required]
    public int MaxParticipants { get; set; } = 100;

    [Required, Column(TypeName = "decimal(8,2)")]
    public decimal EntryFee { get; set; }

    public RaceRoute? Route { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}