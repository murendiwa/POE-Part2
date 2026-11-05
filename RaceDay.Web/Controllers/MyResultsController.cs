using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyResultsController(IRaceDayApi api) : BaseController
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<MyResultDto>>("api/results/mine");
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(new List<MyResultDto>());
        }

        return View((result.Data ?? []).OrderByDescending(r => r.EventDate).ToList());
    }
}