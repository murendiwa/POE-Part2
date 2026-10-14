using RaceDay.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class ResultRequest
{
    /// <summary>Format "hh:mm:ss", e.g. "00:48:32". Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    public ResultStatus Status { get; set; } = ResultStatus.Finished;
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int CategoryId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public ResultStatus Status { get; set; }
    public DateTime RecordedAt { get; set; }
}