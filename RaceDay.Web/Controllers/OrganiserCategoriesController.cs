using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserCategoriesController(IRaceDayApi api) : OrganiserBaseController(api)
{
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect is not null) return redirect;
        return View(BuildPage(ev!, new CategoryFormViewModel { EventId = eventId }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        var (ev, redirect) = await LoadOwnedEventAsync(model.EventId);
        if (redirect is not null) return redirect;
        if (!ModelState.IsValid) return View("Index", BuildPage(ev!, model));

        var result = await Api.PostAsync<CategoryRequestDto, CategoryDto>(
            $"api/events/{model.EventId}/categories", ToRequest(model));
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            ModelState.AddModelError("", result.Error!);
            return View("Index", BuildPage(ev!, model));
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cat = await Api.GetAsync<CategoryDto>($"api/categories/{id}");
        if (!cat.Success || cat.Data is null)
        {
            if (SessionLost) return SessionExpired(cat.Error ?? "Your session has expired. Please log in again.");
            TempData["Error"] = cat.Error ?? "We couldn't find that category.";
            return RedirectToAction("Index", "OrganiserEvents");
        }

        var (ev, redirect) = await LoadOwnedEventAsync(cat.Data.EventId);   // also checks ownership
        if (redirect is not null) return redirect;

        var c = cat.Data;
        return View(new CategoryFormViewModel
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            EventName = ev!.Name,
            Name = c.Name ?? "",
            DistanceKm = c.DistanceKm,
            MaxParticipants = c.MaxParticipants,
            EntryFee = c.EntryFee
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PutAsync<CategoryRequestDto>($"api/categories/{model.CategoryId}", ToRequest(model));
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = result.Error;   // e.g. can't delete: people have enrolled

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private static CategoriesPageViewModel BuildPage(EventDetailDto ev, CategoryFormViewModel form) =>
        new() { Event = ev, Categories = ev.Categories ?? [], NewCategory = form };

    private static CategoryRequestDto ToRequest(CategoryFormViewModel m) =>
        new(m.Name, m.DistanceKm, m.MaxParticipants, m.EntryFee);
}