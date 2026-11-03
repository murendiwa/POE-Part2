using Microsoft.AspNetCore.Mvc;
using RaceDay.Filters;
using RaceDay.Services;
using RaceDay.Web.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserResultsController(IRaceDayApi api) : OrganiserBaseController(api)
{
    private static readonly Regex TimePattern = new(@"^\d{1,2}:[0-5]\d:[0-5]\d$");

    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect is not null) return redirect;

        var enrolResult = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (!enrolResult.Success)
        {
            if (SessionLost) return SessionExpired(enrolResult.Error!);
            TempData["Error"] = enrolResult.Error;
            return RedirectToAction("Index", "OrganiserEvents");
        }

        // Cancelled enrolments don't race.
        var enrolments = (enrolResult.Data ?? []).Where(e => e.Status != "Cancelled").ToList();

        var groups = new List<ResultGroup>();
        foreach (var cat in ev!.Categories ?? [])
        {
            // The category leaderboard tells us which enrolments already have a result.
            var board = await Api.GetAsync<List<ResultDto>>($"api/categories/{cat.CategoryId}/results");
            var byEnrolment = (board.Data ?? [])
                .GroupBy(r => r.EnrolmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var rows = enrolments
                .Where(e => e.CategoryId == cat.CategoryId)
                .Select(e => new ResultRow { Enrolment = e, Result = byEnrolment.GetValueOrDefault(e.EnrolmentId) })
                .OrderBy(r => r.Result?.Position ?? int.MaxValue)
                .ThenBy(r => r.Enrolment.ParticipantName)
                .ToList();

            groups.Add(new ResultGroup { Category = cat, Rows = rows });
        }

        return View(new ResultsPageViewModel { Event = ev, Groups = groups });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int enrolmentId, int eventId, bool hasResult,
        string status, string? finishTime, int? position)
    {
        if (!ResultsPageViewModel.Outcomes.Contains(status))
            return Fail(eventId, "Please choose Finished, DNF or DNS.");

        TimeSpan? time = null;
        if (status == "Finished")
        {
            var text = (finishTime ?? "").Trim();
            if (!TimePattern.IsMatch(text) || !TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var parsed))
                return Fail(eventId, "Enter the finish time as hh:mm:ss, for example 01:45:30.");
            if (position is < 1)
                return Fail(eventId, "The finishing position must be 1 or higher.");
            time = parsed;
        }
        else
        {
            position = null;   // DNF and DNS have no time or position
        }

        var dto = new ResultRequestDto(time, position, status);
        var url = $"api/enrolments/{enrolmentId}/result";

        bool ok;
        string? error;
        if (hasResult)
        {
            var r = await Api.PutAsync<ResultRequestDto>(url, dto);
            ok = r.Success; error = r.Error;
        }
        else
        {
            var r = await Api.PostAsync<ResultRequestDto, ResultDto>(url, dto);
            ok = r.Success; error = r.Error;
        }

        if (!ok && SessionLost) return SessionExpired(error!);

        if (ok) TempData["Success"] = "Result saved and published.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResult(int enrolmentId, int eventId)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{enrolmentId}/result");
        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Result removed.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // Once the race is over, mark the event Completed.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int eventId)
    {
        var (ev, redirect) = await LoadOwnedEventAsync(eventId);
        if (redirect is not null) return redirect;

        var request = new EventRequestDto(ev!.Name ?? "", ev.Description, ev.EventDate, ev.Location ?? "",
            ev.Province, "Completed", ev.EventType ?? "", ev.DistanceKm);

        var result = await Api.PutAsync<EventRequestDto>($"api/events/{eventId}", request);
        if (!result.Success && SessionLost) return SessionExpired(result.Error!);

        if (result.Success) TempData["Success"] = "Event marked as completed.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private IActionResult Fail(int eventId, string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index), new { eventId });
    }
}