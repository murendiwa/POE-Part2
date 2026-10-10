using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;

    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime EnrolmentDate { get; set; }

    [Required]
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Registered;

    [Required]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public Result? Result { get; set; }
}