using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Controllers;

public class AccountController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.LoginAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        // Later (Steps 3-4) send Organisers to Organiser/Dashboard and Participants to Events/Index.
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role is not ("Participant" or "Organiser"))
            ModelState.AddModelError(nameof(model.Role), "Please choose Participant or Organiser.");

        if (!ModelState.IsValid) return View(model);

        var result = await api.RegisterAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await api.LogoutAsync();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private bool IsLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString("Role"));
}