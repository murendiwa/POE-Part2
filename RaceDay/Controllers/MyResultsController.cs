using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using System.Security.Claims;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/results")]
public class MyResultsController(AppDbContext db) : ControllerBase   // TODO-ADAPT context name
{
    /// <summary>The logged-in participant's race history, with total finishers per category.</summary>
    [Authorize(Roles = "Participant")]
    [HttpGet("mine")]
    public async Task<ActionResult<List<MyResultResponse>>> Mine()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // TODO-ADAPT: navigation names (Enrolment -> Category -> Event) and key names
        return await db.Results
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(r => new MyResultResponse
            {
                ResultId = r.ResultId,
                EventId = r.Enrolment.Category.EventId,
                EventName = r.Enrolment.Category.Event.Name,
                EventDate = r.Enrolment.Category.Event.EventDate,
                CategoryId = r.Enrolment.CategoryId,
                CategoryName = r.Enrolment.Category.Name,
                FinishTime = r.FinishTime,
                Position = r.Position,
                Status = r.Status,
                TotalFinishers = db.Results.Count(x =>
                    x.Enrolment.CategoryId == r.Enrolment.CategoryId && x.Status == ResultStatus.Finished)
            })
            .ToListAsync();
    }
}