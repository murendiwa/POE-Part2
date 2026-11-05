namespace RaceDay.Web.Models;

public record EventDto(int EventId, int OrganiserId, string? OrganiserName, string? Name, string? Description,
    DateTime EventDate, string? Location, string? Province, string Status, DateTime CreatedAt,
    string? EventType, decimal DistanceKm, string? BannerUrl);

public record CategorySummaryDto(int CategoryId, string? Name, decimal DistanceKm, int MaxParticipants, decimal EntryFee);

public record EventDetailDto(int EventId, int OrganiserId, string? OrganiserName, string? Name, string? Description,
    DateTime EventDate, string? Location, string? Province, string Status, DateTime CreatedAt,
    string? EventType, decimal DistanceKm, string? BannerUrl, List<CategorySummaryDto>? Categories);

public record EnrolmentDto(int EnrolmentId, int ParticipantId, string? ParticipantName, int CategoryId,
    string? CategoryName, int EventId, string? EventName, DateTime EventDate, decimal EntryFee,
    DateTime EnrolmentDate, string Status, string PaymentStatus);

public record OrganiserEventDto(int EventId, string? Name, string? Description, DateTime EventDate,
    string? Location, string? Province, string Status, string? EventType, decimal DistanceKm,
    string? BannerUrl, int EnrolmentCount);

public record EventRequestDto(string Name, string? Description, DateTime EventDate, string Location,
    string? Province, string Status, string EventType, decimal DistanceKm);

public record CategoryDto(int CategoryId, int EventId, string? Name, decimal DistanceKm, int MaxParticipants, decimal EntryFee);
public record CategoryRequestDto(string Name, decimal DistanceKm, int MaxParticipants, decimal EntryFee);
public record EnrolmentUpdateRequestDto(string Status, string PaymentStatus);
public record ResultDto(int ResultId, int EnrolmentId, int CategoryId, string? ParticipantName,
    TimeSpan? FinishTime, int? Position, string Status, DateTime RecordedAt);
public record ResultRequestDto(TimeSpan? FinishTime, int? Position, string Status);
public record MyResultDto(int ResultId, int EventId, string? EventName, DateTime EventDate, int CategoryId,
    string? CategoryName, TimeSpan? FinishTime, int? Position, int TotalFinishers, string Status);

public record ProfileDto(int UserId, string? FullName, string? Email, string? PhoneNumber,
    DateTime? DateOfBirth, string? ProfilePictureUrl, string Role);

// The date goes to the API as "yyyy-MM-dd" text, so it works whether the API stores DateOnly or DateTime.
public record ProfileUpdateRequestDto(string FullName, string? PhoneNumber, string? DateOfBirth);