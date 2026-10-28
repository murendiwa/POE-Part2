using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Result
{
    public int ResultId { get; set; }
    [Required]
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    [Required]
    public ResultStatus Status { get; set; } = ResultStatus.Finished;
    public DateTime StartTime { get; set; }
}