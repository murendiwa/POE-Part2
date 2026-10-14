using RaceDay.Models;

namespace RaceDay.DTOs;

public class EnrolmentUpdateRequest
{
    public EnrolmentStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal EntryFee { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public EnrolmentStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}