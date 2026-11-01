using Microsoft.AspNetCore.Mvc;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class EventsController(IRaceDayApi api) : BaseController
{
    // Home page for Participants: upcoming events + filters
    public async Task<IActionResult> Index(string? search, string? province, DateTime? from, DateTime? to)
    {
        var vm = new EventListViewModel { Search = search, Province = province, From = from, To = to };

        // The API filters by status and province; the rest is just narrowing the list for display.
        var url = "api/events?status=Scheduled";
        if (!string.IsNullOrWhiteSpace(province)) url += $"&province={Uri.EscapeDataString(province)}";

        var result = await api.GetAsync<List<EventDto>>(url);
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(vm);
        }

        var events = (result.Data ?? []).Where(e => e.EventDate.Date >= DateTime.Today);

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e =>
                (e.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Location ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue) events = events.Where(e => e.EventDate.Date >= from.Value.Date);
        if (to.HasValue) events = events.Where(e => e.EventDate.Date <= to.Value.Date);

        vm.Events = events.OrderBy(e => e.EventDate).ToList();
        return View(vm);
    }

    // Full event information + categories
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!eventResult.Success || eventResult.Data is null)
        {
            if (SessionLost) return SessionExpired(eventResult.Error ?? "Your session has expired. Please log in again.");
            TempData["Error"] = eventResult.Error ?? "We couldn't find that event.";
            return RedirectToAction(nameof(Index));
        }

        // Which categories is this participant already in? (so we can show "Enrolled" instead of a button)
        var mine = await api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        var enrolled = (mine.Data ?? [])
            .Where(e => e.Status != "Cancelled")
            .Select(e => e.CategoryId)
            .ToHashSet();

        return View(new EventDetailsViewModel { Event = eventResult.Data, EnrolledCategoryIds = enrolled });
    }

    // Enrol in a category: recorded immediately by the API
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await api.PostAsync<object, EnrolmentDto>(
            $"api/categories/{categoryId}/enrolments", new object());

        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;   // e.g. already enrolled, category full
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = $"You're in! You are enrolled in {result.Data?.CategoryName ?? "the category"}.";
        return RedirectToAction("Index", "MyEnrolments");
    }
}