using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/categories/{categoryId:int}/route")]
public class CategoryRoutesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoryRoutesController(RaceDayDbContext db) => _db = db;

    /// <summary>Get the route of a category. Each category has at most one route.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int categoryId)
    {
        var route = await _db.Routes
            .AsNoTracking()
            .Where(r => r.CategoryId == categoryId)
            .Select(r => new RouteResponse
            {
                RouteId = r.RouteId,
                CategoryId = r.CategoryId,
                RouteName = r.RouteName,
                StartPoint = r.StartPoint,
                EndPoint = r.EndPoint,
                ElevationGainM = r.ElevationGainM,
                GpxFileUrl = r.GpxFileUrl,
                Description = r.Description
            })
            .FirstOrDefaultAsync();

        return route is null ? NotFound(new { message = "This category has no route." }) : Ok(route);
    }

    /// <summary>Create the route for a category. Only the organiser who owns the event may do this.</summary>
    [HttpPost]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int categoryId, RouteRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        if (await _db.Routes.AnyAsync(r => r.CategoryId == categoryId))
            return Conflict(new { message = "This category already has a route. Use PUT to change it." });

        var route = new RaceRoute
        {
            CategoryId = categoryId,
            RouteName = request.RouteName,
            StartPoint = request.StartPoint.Trim(),
            EndPoint = request.EndPoint.Trim(),
            ElevationGainM = request.ElevationGainM,
            GpxFileUrl = request.GpxFileUrl,
            Description = request.Description
        };

        _db.Routes.Add(route);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { categoryId }, ToResponse(route));
    }

    /// <summary>Update the route of a category. Only the organiser who owns the event may do this.</summary>
    [HttpPut]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int categoryId, RouteRequest request)
    {
        var route = await _db.Routes
            .Include(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.CategoryId == categoryId);
        if (route is null) return NotFound(new { message = "This category has no route." });

        if (route.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        route.RouteName = request.RouteName;
        route.StartPoint = request.StartPoint.Trim();
        route.EndPoint = request.EndPoint.Trim();
        route.ElevationGainM = request.ElevationGainM;
        route.GpxFileUrl = request.GpxFileUrl;
        route.Description = request.Description;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(route));
    }

    /// <summary>Delete the route of a category. Only the organiser who owns the event may do this.</summary>
    [HttpDelete]
    [RequireRole(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var route = await _db.Routes
            .Include(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.CategoryId == categoryId);
        if (route is null) return NotFound(new { message = "This category has no route." });

        if (route.Category.Event.OrganiserId != HttpContext.Session.GetUserId()!.Value)
            return NotOwner();

        _db.Routes.Remove(route);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ObjectResult NotOwner() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only modify routes of your own events." });

    private static RouteResponse ToResponse(RaceRoute r) => new()
    {
        RouteId = r.RouteId,
        CategoryId = r.CategoryId,
        RouteName = r.RouteName,
        StartPoint = r.StartPoint,
        EndPoint = r.EndPoint,
        ElevationGainM = r.ElevationGainM,
        GpxFileUrl = r.GpxFileUrl,
        Description = r.Description
    };
}