using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserController(IRaceDayApi api) : BaseController
{
    public async Task<IActionResult> Dashboard()
    {
        var result = await api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(new DashboardViewModel());
        }

        var events = result.Data ?? [];
        return View(new DashboardViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            Upcoming = events.Where(e => e.Status == "Scheduled" && e.EventDate >= DateTime.Now)
                             .OrderBy(e => e.EventDate).ToList(),
            TotalEnrolments = events.Sum(e => e.EnrolmentCount)
        });
    }
}