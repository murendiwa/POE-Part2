using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrol the logged-in participant in a category. Fails if the event is not open, the category is full, or you are already enrolled.</summary>
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [RequireRole(UserRole.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var participantId = HttpContext.Session.GetUserId()!.Value;

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.Status != EventStatus.Scheduled || category.Event.EventDate <= DateTime.Now)
            return BadRequest(new { message = "This event is not open for enrolment." });

        var existing = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.ParticipantId == participantId && e.CategoryId == categoryId);

        if (existing is not null && existing.Status != EnrolmentStatus.Cancelled)
            return Conflict(new { message = "You are already enrolled in this category." });

        var activeCount = await _db.Enrolments
            .CountAsync(e => e.CategoryId == categoryId && e.Status != EnrolmentStatus.Cancelled);
        if (activeCount >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        Enrolment enrolment;
        if (existing is not null)
        {
            // The UNIQUE (ParticipantId, CategoryId) constraint allows one row per pair, so re-enrolling reactivates the cancelled row.
            existing.Status = EnrolmentStatus.Registered;
            existing.PaymentStatus = PaymentStatus.Pending;
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment { ParticipantId = participantId, CategoryId = categoryId };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();

        var response = await Project(_db.Enrolments.AsNoTracking().Where(e => e.EnrolmentId == enrolment.EnrolmentId))
            .FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = enrolment.EnrolmentId }, response);
    }

    /// <summary>List the logged-in participant's own enrolments.</summary>
    [HttpGet("enrolments/mine")]
    [RequireRole(UserRole.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetUserId()!.Value;

        var list = await Project(_db.Enrolments.AsNoTracking()
                .Where(e => e.ParticipantId == userId)
                .OrderByDescending(e => e.EnrolmentDate))
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Get one enrolment. Visible to the participant who owns it and to the organiser of its event.</summary>
    [HttpGet("enrolments/{id:int}")]
    [RequireRole]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = HttpContext.Session.GetUserId()!.Value;

        var enrolment = await _db.Enrolments.AsNoTracking()
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != userId && enrolment.Category.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view your own enrolments." });

        var response = await Project(_db.Enrolments.AsNoTracking().Where(e => e.EnrolmentId == id)).FirstAsync();
        return Ok(response);
    }

    /// <summary>List all enrolments for an event. Only the organiser who owns the event may do this. Optionally filter by status.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId, [FromQuery] EnrolmentStatus? status)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for your own events." });

        var query = _db.Enrolments.AsNoTracking().Where(e => e.Category.EventId == eventId);
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var list = await Project(query.OrderBy(e => e.CategoryId).ThenBy(e => e.EnrolmentDate)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancel your own enrolment. A paid enrolment is marked Refunded.</summary>
    [HttpPut("enrolments/{id:int}/cancel")]
    [RequireRole(UserRole.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != HttpContext.Session.GetUserId()!.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only cancel your own enrolments." });

        if (enrolment.Status == EnrolmentStatus.Cancelled)
            return Conflict(new { message = "This enrolment is already cancelled." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { message = "A result has already been recorded, so this enrolment cannot be cancelled." });

        enrolment.Status = EnrolmentStatus.Cancelled;
        if (enrolment.PaymentStatus == PaymentStatus.Paid)
            enrolment.PaymentStatus = PaymentStatus.Refunded;

        await _db.SaveChangesAsync();

        var response = await Project(_db.Enrolments.AsNoTracking().Where(e => e.EnrolmentId == id)).FirstAsync();
        return Ok(response);
    }

    /// <summary>Update an enrolment's status and payment status (e.g. confirm and mark as paid). Only the organiser who owns the event may do this.</summary>
    [HttpPut("enrolments/{id:int}")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, EnrolmentUpdateRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage enrolments for your own events." });

        if (request.Status == EnrolmentStatus.Cancelled && await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { message = "Delete the result first before cancelling this enrolment." });

        enrolment.Status = request.Status;
        enrolment.PaymentStatus = request.PaymentStatus;

        await _db.SaveChangesAsync();

        var response = await Project(_db.Enrolments.AsNoTracking().Where(e => e.EnrolmentId == id)).FirstAsync();
        return Ok(response);
    }

    private static IQueryable<EnrolmentResponse> Project(IQueryable<Enrolment> query) =>
        query.Select(e => new EnrolmentResponse
        {
            EnrolmentId = e.EnrolmentId,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant.FullName,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            EventId = e.Category.EventId,
            EventName = e.Category.Event.Name,
            EventDate = e.Category.Event.EventDate,
            EntryFee = e.Category.EntryFee,
            EnrolmentDate = e.EnrolmentDate,
            Status = e.Status,
            PaymentStatus = e.PaymentStatus
        });
}