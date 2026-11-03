using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserEventsController(IRaceDayApi api) : BaseController
{
    private const long MaxBannerBytes = 5 * 1024 * 1024;

    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(new List<OrganiserEventDto>());
        }
        return View((result.Data ?? []).OrderByDescending(e => e.EventDate).ToList());
    }

    // ---------- Create ----------
    [HttpGet]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        CheckBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        var result = await api.PostAsync<EventRequestDto, EventDto>("api/events", ToRequest(model, "Scheduled"));
        if (!result.Success || result.Data is null)
        {
            if (SessionLost) return SessionExpired(result.Error ?? "Your session has expired. Please log in again.");
            ModelState.AddModelError("", result.Error ?? "We couldn't create the event. Please try again.");
            return View("Form", model);
        }

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await api.UploadImageAsync($"api/events/{result.Data.EventId}/banner", model.BannerFile);
            if (!upload.Success)
            {
                TempData["Error"] = $"Event created, but the banner wasn't uploaded: {upload.Error}";
                return RedirectToAction(nameof(Edit), new { id = result.Data.EventId });
            }
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Edit ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!result.Success || result.Data is null)
        {
            if (SessionLost) return SessionExpired(result.Error ?? "Your session has expired. Please log in again.");
            TempData["Error"] = result.Error ?? "We couldn't find that event.";
            return RedirectToAction(nameof(Index));
        }

        var ev = result.Data;
        if (ev.OrganiserId.ToString() != HttpContext.Session.GetString("UserId"))
            return RedirectToAction("AccessDenied", "Account");   // not your event

        return View("Form", new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name ?? "",
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location ?? "",
            Province = ev.Province,
            EventType = ev.EventType ?? "",
            DistanceKm = ev.DistanceKm,
            Status = ev.Status,
            ExistingBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormViewModel model)
    {
        CheckBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        var result = await api.PutAsync($"api/events/{model.EventId}", ToRequest(model, model.Status));
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            ModelState.AddModelError("", result.Error!);
            return View("Form", model);
        }

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await api.UploadImageAsync($"api/events/{model.EventId}/banner", model.BannerFile);
            if (!upload.Success)
            {
                ModelState.AddModelError("", $"Your changes were saved, but the banner wasn't uploaded: {upload.Error}");
                return View("Form", model);
            }
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Delete ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await api.DeleteAsync($"api/events/{id}");
        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Error;   // e.g. can't delete: people have enrolled

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------
    private static EventRequestDto ToRequest(EventFormViewModel m, string status) =>
        new(m.Name, m.Description, m.EventDate, m.Location, m.Province, status, m.EventType, m.DistanceKm);

    // Friendly early check; the API validates the file again.
    private void CheckBanner(EventFormViewModel model)
    {
        if (model.BannerFile is not { Length: > 0 } file) return;
        if (file.Length > MaxBannerBytes)
            ModelState.AddModelError(nameof(model.BannerFile), "The image must be 5 MB or smaller.");
        else if (!new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
            ModelState.AddModelError(nameof(model.BannerFile), "Only JPG, PNG or WEBP images are allowed.");
    }
}