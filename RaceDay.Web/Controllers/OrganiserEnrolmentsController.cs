using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserEnrolmentsController(IRaceDayApi api) : OrganiserBaseController(api)
{
    public async Task<IActionResult> Index(int eventId, string? status)
    {
        var (ev, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect is not null) return redirect;

        var url = $"api/events/{eventId}/enrolments";
        if (!string.IsNullOrWhiteSpace(status)) url += $"?status={Uri.EscapeDataString(status)}";

        var vm = new EventEnrolmentsViewModel { Event = ev!, StatusFilter = status };

        var result = await Api.GetAsync<List<EnrolmentDto>>(url);
        if (!result.Success)
        {
            if (SessionLost) return SessionExpired(result.Error!);
            TempData["Error"] = result.Error;
            return View(vm);
        }

        vm.Enrolments = (result.Data ?? []).OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int eventId, string status, string paymentStatus, string? filter)
    {
        if (!EventEnrolmentsViewModel.EnrolmentStatuses.Contains(status) ||
            !EventEnrolmentsViewModel.PaymentStatuses.Contains(paymentStatus))
        {
            TempData["Error"] = "Please choose a valid status and payment status.";
            return RedirectToAction(nameof(Index), new { eventId, status = filter });
        }

        var result = await Api.PutAsync<EnrolmentUpdateRequestDto>(
            $"api/enrolments/{id}", new EnrolmentUpdateRequestDto(status, paymentStatus));

        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Enrolment updated.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { eventId, status = filter });
    }
}