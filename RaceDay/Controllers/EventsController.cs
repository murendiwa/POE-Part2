using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>List all events (public). Optionally filter by province and/or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? province, [FromQuery] EventStatus? status)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(e => e.Province == province);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventResponse
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FullName,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Province = e.Province,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get one event with its categories (public).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventId == id)
            .Select(e => new EventDetailResponse
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FullName,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Province = e.Province,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                Categories = e.Categories.Select(c => new CategorySummary
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    DistanceKm = c.DistanceKm,
                    MaxParticipants = c.MaxParticipants,
                    EntryFee = c.EntryFee
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return ev is null ? NotFound(new { message = "Event not found." }) : Ok(ev);
    }

    /// <summary>Create an event. Organisers only. The event is owned by the logged-in organiser.</summary>
    [HttpPost]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (request.EventDate <= DateTime.Now)
            return BadRequest(new { message = "Event date must be in the future." });

        var ev = new Event
        {
            OrganiserId = HttpContext.Session.GetUserId()!.Value,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Province = request.Province,
            Status = request.Status
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToResponse(ev));
    }

    /// <summary>Update an event. Only the organiser who created it may do this.</summary>
    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Province = request.Province;
        ev.Status = request.Status;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>Delete an event. Only its organiser may do this, and only if nobody has enrolled.</summary>
    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category.EventId == id);
        if (hasEnrolments)
            return Conflict(new { message = "This event cannot be deleted. Set its status to Cancelled instead." });

        _db.Events.Remove(ev); // categories and routes cascade, as in the Part 1 schema
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ObjectResult NotOwner() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only modify your own events." });

    private static EventResponse ToResponse(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Province = e.Province,
        Status = e.Status,
        CreatedAt = e.CreatedAt
    };
}