using Microsoft.AspNetCore.Mvc;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public abstract class OrganiserBaseController(IRaceDayApi api) : BaseController
{
    protected IRaceDayApi Api { get; } = api;

    protected async Task<(EventDetailDto? Event, IActionResult? Redirect)> LoadOwnedEventAsync(int eventId)
    {
        var result = await Api.GetAsync<EventDetailDto>($"api/events/{eventId}");
        if (!result.Success || result.Data is null)
        {
            if (SessionLost)
                return (null, SessionExpired(result.Error ?? "Your session has expired. Please log in again."));

            TempData["Error"] = result.Error ?? "We couldn't find that event.";
            return (null, RedirectToAction("Index", "OrganiserEvents"));
        }

        if (result.Data.OrganiserId.ToString() != HttpContext.Session.GetString("UserId"))
            return (null, RedirectToAction("AccessDenied", "Account"));

        return (result.Data, null);
    }
}
