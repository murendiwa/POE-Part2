using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Public leaderboard for a category, ordered by position.</summary>
    [HttpGet("categories/{categoryId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId))
            return NotFound(new { message = "Category not found." });

        var results = await Project(_db.Results.AsNoTracking()
                .Where(r => r.Enrolment.CategoryId == categoryId)
                .OrderBy(r => r.Position == null)
                .ThenBy(r => r.Position))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Get the result for one enrolment. Visible to the participant who owns it and to the organiser of the event.</summary>
    [HttpGet("enrolments/{enrolmentId:int}/result")]
    [RequireRole]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int enrolmentId)
    {
        var userId = HttpContext.Session.GetUserId()!.Value;

        var enrolment = await LoadEnrolment(enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != userId && enrolment.Category.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view your own result." });

        var result = await Project(_db.Results.AsNoTracking().Where(r => r.EnrolmentId == enrolmentId))
            .FirstOrDefaultAsync();

        return result is null ? NotFound(new { message = "No result has been recorded yet." }) : Ok(result);
    }

    /// <summary>Record the result for an enrolment. Only the organiser who owns the event may do this. Finished results need a FinishTime; DNF/DNS must not have a FinishTime or Position.</summary>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, ResultRequest request)
    {
        var enrolment = await LoadEnrolment(enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        if (enrolment.Status == EnrolmentStatus.Cancelled)
            return BadRequest(new { message = "Cannot record a result for a cancelled enrolment." });

        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment. Use PUT to change it." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await Project(_db.Results.AsNoTracking().Where(r => r.ResultId == result.ResultId)).FirstAsync();
        return CreatedAtAction(nameof(Get), new { enrolmentId }, response);
    }

    /// <summary>Correct an existing result. Only the organiser who owns the event may do this.</summary>
    [HttpPut("enrolments/{enrolmentId:int}/result")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int enrolmentId, ResultRequest request)
    {
        var enrolment = await LoadEnrolment(enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);
        if (result is null) return NotFound(new { message = "No result has been recorded yet." });

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;

        await _db.SaveChangesAsync();

        var response = await Project(_db.Results.AsNoTracking().Where(r => r.ResultId == result.ResultId)).FirstAsync();
        return Ok(response);
    }

    /// <summary>Delete a result. Only the organiser who owns the event may do this.</summary>
    [HttpDelete("enrolments/{enrolmentId:int}/result")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int enrolmentId)
    {
        var enrolment = await LoadEnrolment(enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);
        if (result is null) return NotFound(new { message = "No result has been recorded yet." });

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Task<Enrolment?> LoadEnrolment(int enrolmentId) =>
        _db.Enrolments.AsNoTracking()
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

    private static string? Validate(ResultRequest r)
    {
        if (r.Status == ResultStatus.Finished)
        {
            if (r.FinishTime is null || r.FinishTime <= TimeSpan.Zero)
                return "A Finished result needs a FinishTime greater than zero.";
            if (r.FinishTime >= TimeSpan.FromHours(24))
                return "FinishTime must be under 24 hours.";
        }
        else if (r.FinishTime is not null || r.Position is not null)
        {
            return "DNF and DNS results cannot have a FinishTime or Position.";
        }
        return null;
    }

    private ObjectResult NotOwner() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage results for your own events." });

    private static IQueryable<ResultResponse> Project(IQueryable<Result> query) =>
        query.Select(r => new ResultResponse
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            CategoryId = r.Enrolment.CategoryId,
            ParticipantName = r.Enrolment.Participant.FullName,
            FinishTime = r.FinishTime,
            Position = r.Position,
            Status = r.Status,
            RecordedAt = r.StartTime
        });
}
