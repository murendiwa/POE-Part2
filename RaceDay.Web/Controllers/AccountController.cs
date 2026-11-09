using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Web.Controllers;

public class AccountController(IRaceDayApi api) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        if (!IsLoggedIn) return View(new LoginViewModel());
        return HttpContext.Session.GetString("Role") == "Participant"
            ? RedirectToAction("Index", "Events")
            : RedirectToAction("Dashboard", "Organiser");
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

        return result.Data?.Role == "Participant"
            ? RedirectToAction("Index", "Events")
            : RedirectToAction("Dashboard", "Organiser");
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