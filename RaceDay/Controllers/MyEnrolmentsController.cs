using Microsoft.AspNetCore.Mvc;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyEnrolmentsController(IRaceDayApi api) : BaseController
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(new List<EnrolmentDto>());
        }

        return View((result.Data ?? []).OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.PutAsync<object>($"api/enrolments/{id}/cancel", new object());

        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}