using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    // The API session expired.
    protected bool SessionLost => string.IsNullOrEmpty(HttpContext.Session.GetString("Role"));

    protected IActionResult SessionExpired(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction("Login", "Account");
    }
}
