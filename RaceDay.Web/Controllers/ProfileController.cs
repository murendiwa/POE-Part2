using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class ProfileController(IRaceDayApi api) : BaseController
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<ProfileDto>("api/users/me");
        if (!result.Success || result.Data is null)
        {
            if (SessionLost) return SessionExpired(result.Error ?? "Your session has expired. Please log in again.");
            TempData["Error"] = result.Error ?? "We couldn't load your profile.";
            return RedirectToAction("Index", "Events");
        }

        var p = result.Data;
        return View(new ProfileViewModel
        {
            FullName = p.FullName ?? "",
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            ProfilePictureUrl = p.ProfilePictureUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (model.DateOfBirth is { } dob && dob.Date > DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "The date of birth can't be in the future.");

        if (!ModelState.IsValid) return View("Index", model);

        var request = new ProfileUpdateRequestDto(
            model.FullName.Trim(),
            string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            model.DateOfBirth?.ToString("yyyy-MM-dd"));

        var result = await api.PutAsync<ProfileUpdateRequestDto>("api/users/me", request);
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            ModelState.AddModelError("", result.Error!);
            return View("Index", model);
        }

        HttpContext.Session.SetString("UserName", request.FullName);   // keep the navbar name current
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        string? problem = ImageRules.Check(picture);
        if (problem is not null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Index));
        }

        var result = await api.UploadImageAsync("api/users/me/picture", picture!);
        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Profile picture updated.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}