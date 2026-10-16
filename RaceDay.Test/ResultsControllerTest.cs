using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Controllers;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Test;

public class ResultsControllerTests
{
    private static async Task<(User Organiser, User Participant, Category Category, Enrolment Enrolment)> SeedAsync(
        RaceDayDbContext db, EnrolmentStatus status = EnrolmentStatus.Confirmed)
    {
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var participant = await db.AddUserAsync(UserRole.Participant);
        var ev = await db.AddEventAsync(organiser);
        var category = await db.AddCategoryAsync(ev);
        var enrolment = await db.AddEnrolmentAsync(participant, category, status);
        return (organiser, participant, category, enrolment);
    }

    private static ResultRequest FinishedRequest() => new()
    {
        FinishTime = new TimeSpan(0, 48, 32),
        Position = 1,
        Status = ResultStatus.Finished
    };

    // ---------- Create ----------

    [Fact]
    public async Task CreateFinishedResultByOwningOrganiserReturns201AndSaves()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId, FinishedRequest());

        Assert.IsType<CreatedAtActionResult>(result);
        var stored = await db.Results.SingleAsync();
        Assert.Equal(enrolment.EnrolmentId, stored.EnrolmentId);
        Assert.Equal(1, stored.Position);
        Assert.Equal(new TimeSpan(0, 48, 32), stored.FinishTime);
    }

    [Fact]
    public async Task CreateDnfResultWithoutTimeOrPositionReturns201()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId, new ResultRequest { Status = ResultStatus.DNF });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task CreateByAnotherOrganiserReturns403AndSavesNothing()
    {
        using var db = TestHelpers.CreateDb();
        var (_, _, _, enrolment) = await SeedAsync(db);
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new ResultsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId, FinishedRequest());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, await db.Results.CountAsync());
    }

    [Fact]
    public async Task CreateUnknownEnrolmentReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(999, FinishedRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateForACancelledEnrolmentReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db, EnrolmentStatus.Cancelled);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId, FinishedRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateFinishedWithoutAFinishTimeReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId,
            new ResultRequest { Status = ResultStatus.Finished, Position = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.Results.CountAsync());
    }

    [Fact]
    public async Task CreateFinishTimeOf24HoursOrMoreReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId,
            new ResultRequest { Status = ResultStatus.Finished, FinishTime = TimeSpan.FromHours(24) });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDnfWithAFinishTimeReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId,
            new ResultRequest { Status = ResultStatus.DNF, FinishTime = new TimeSpan(0, 30, 0) });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateWhenAResultAlreadyExistsReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(enrolment.EnrolmentId, FinishedRequest());

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, await db.Results.CountAsync());
    }

    // ---------- Reading results ----------

    [Fact]
    public async Task GetByTheOwningParticipantReturns200()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var controller = new ResultsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Get(enrolment.EnrolmentId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByAnUnrelatedParticipantReturns403()
    {
        using var db = TestHelpers.CreateDb();
        var (_, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var stranger = await db.AddUserAsync(UserRole.Participant);
        var controller = new ResultsController(db).WithSession(stranger.UserId, stranger.Role);

        var result = await controller.Get(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetWhenNoResultRecordedYetReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Get(enrolment.EnrolmentId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetForCategoryIsPublicAndOrderedByPositionWithUnrankedLast()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(organiser);
        var category = await db.AddCategoryAsync(ev);
        var p1 = await db.AddUserAsync(UserRole.Participant);
        var p2 = await db.AddUserAsync(UserRole.Participant);
        var p3 = await db.AddUserAsync(UserRole.Participant);
        var e1 = await db.AddEnrolmentAsync(p1, category, EnrolmentStatus.Confirmed);
        var e2 = await db.AddEnrolmentAsync(p2, category, EnrolmentStatus.Confirmed);
        var e3 = await db.AddEnrolmentAsync(p3, category, EnrolmentStatus.Confirmed);
        await db.AddResultAsync(e1, position: 2, finishTime: new TimeSpan(1, 0, 0));
        await db.AddResultAsync(e2, position: 1, finishTime: new TimeSpan(0, 50, 0));
        await db.AddResultAsync(e3, status: ResultStatus.DNF, position: null, finishTime: null);
        var controller = new ResultsController(db).WithSession(); // anonymous

        var result = await controller.GetForCategory(category.CategoryId);

        var list = Assert.IsType<List<ResultResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(new int?[] { 1, 2, null }, list.Select(r => r.Position).ToArray());
    }

    [Fact]
    public async Task GetForCategoryUnknownCategoryReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new ResultsController(db).WithSession();

        var result = await controller.GetForCategory(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- Update and delete ----------

    [Fact]
    public async Task UpdateByOwningOrganiserCorrectsTheResult()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, position: 5, finishTime: new TimeSpan(1, 0, 0));
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, FinishedRequest());

        Assert.IsType<OkObjectResult>(result);
        var stored = await db.Results.SingleAsync();
        Assert.Equal(1, stored.Position);
        Assert.Equal(new TimeSpan(0, 48, 32), stored.FinishTime);
    }

    [Fact]
    public async Task UpdateByAnotherOrganiserReturns403()
    {
        using var db = TestHelpers.CreateDb();
        var (_, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, position: 5, finishTime: new TimeSpan(1, 0, 0));
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new ResultsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, FinishedRequest());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(5, (await db.Results.SingleAsync()).Position);
    }

    [Fact]
    public async Task UpdateWhenNoResultExistsReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, FinishedRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteByOwningOrganiserReturns204AndRemovesTheResult()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Delete(enrolment.EnrolmentId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await db.Results.CountAsync());
    }

    [Fact]
    public async Task DeleteByAnotherOrganiserReturns403AndKeepsTheResult()
    {
        using var db = TestHelpers.CreateDb();
        var (_, _, _, enrolment) = await SeedAsync(db);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new ResultsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.Delete(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(1, await db.Results.CountAsync());
    }

    [Fact]
    public async Task DeleteWhenNoResultExistsReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, enrolment) = await SeedAsync(db);
        var controller = new ResultsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Delete(enrolment.EnrolmentId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}