using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Controllers;

public class HomeController : Controller
{
    // Public landing page. Replaced later by the Participant home (events + filters).
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}