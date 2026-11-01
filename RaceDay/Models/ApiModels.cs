namespace RaceDay.Web.Models;

public record EventDto(int EventId, int OrganiserId, string? OrganiserName, string? Name, string? Description,
    DateTime EventDate, string? Location, string? Province, string Status, DateTime CreatedAt, string? BannerUrl);

public record CategorySummaryDto(int CategoryId, string? Name, decimal DistanceKm, int MaxParticipants, decimal EntryFee);

public record EventDetailDto(int EventId, int OrganiserId, string? OrganiserName, string? Name, string? Description,
    DateTime EventDate, string? Location, string? Province, string Status, DateTime CreatedAt, string? BannerUrl,
    List<CategorySummaryDto>? Categories);

public record EnrolmentDto(int EnrolmentId, int ParticipantId, string? ParticipantName, int CategoryId,
    string? CategoryName, int EventId, string? EventName, DateTime EventDate, decimal EntryFee,
    DateTime EnrolmentDate, string Status, string PaymentStatus);