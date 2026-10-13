using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>List the categories (e.g. 10km, Half Marathon) of an event (public).</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                DistanceKm = c.DistanceKm,
                MaxParticipants = c.MaxParticipants,
                EntryFee = c.EntryFee
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Get a single category (public).</summary>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryId == id)
            .Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                DistanceKm = c.DistanceKm,
                MaxParticipants = c.MaxParticipants,
                EntryFee = c.EntryFee
            })
            .FirstOrDefaultAsync();

        return category is null ? NotFound(new { message = "Category not found." }) : Ok(category);
    }

    /// <summary>Add a category to an event. Only the organiser who owns the event may do this.</summary>
    [HttpPost("events/{eventId:int}/categories")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Conflict(new { message = "This event already has a category with that name." });

        var category = new Category
        {
            EventId = eventId,
            Name = name,
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, ToResponse(category));
    }

    /// <summary>Update a category. Only the organiser who owns the event may do this.</summary>
    [HttpPut("categories/{id:int}")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == name && c.CategoryId != id))
            return Conflict(new { message = "This event already has a category with that name." });

        var activeEnrolments = await _db.Enrolments
            .CountAsync(en => en.CategoryId == id && en.Status != EnrolmentStatus.Cancelled);
        if (request.MaxParticipants < activeEnrolments)
            return BadRequest(new { message = $"MaxParticipants cannot be lower than the {activeEnrolments} current enrolments." });

        category.Name = name;
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    /// <summary>Delete a category (and its route). Only its event's organiser may do this, and only if nobody has enrolled.</summary>
    [HttpDelete("categories/{id:int}")]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });

        _db.Categories.Remove(category); // the route cascades, as in the Part 1 schema
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ObjectResult NotOwner() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only modify categories of your own events." });

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };
}