using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Controllers;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Test;

public class EnrolmentsControllerTests
{
    private static async Task<(User Organiser, User Participant, Event Ev, Category Category)> SeedAsync(
        RaceDayDbContext db, int maxParticipants = 100,
        EventStatus status = EventStatus.Scheduled, DateTime? date = null)
    {
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var participant = await db.AddUserAsync(UserRole.Participant);
        var ev = await db.AddEventAsync(organiser, status: status, date: date);
        var category = await db.AddCategoryAsync(ev, maxParticipants: maxParticipants);
        return (organiser, participant, ev, category);
    }

    // ---------- Enrol ----------

    [Fact]
    public async Task EnrolAsParticipantCreatesRegisteredPendingEnrolment()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<CreatedAtActionResult>(result);
        var stored = await db.Enrolments.SingleAsync();
        Assert.Equal(participant.UserId, stored.ParticipantId);
        Assert.Equal(EnrolmentStatus.Registered, stored.Status);
        Assert.Equal(PaymentStatus.Pending, stored.PaymentStatus);
    }

    [Fact]
    public async Task EnrolUnknownCategoryReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, _) = await SeedAsync(db);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task EnrolInCancelledEventReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db, status: EventStatus.Cancelled);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.Enrolments.CountAsync());
    }

    [Fact]
    public async Task EnrolInEventThatAlreadyHappenedReturns400()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db, date: DateTime.Now.AddDays(-1));
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task EnrolTwiceReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, await db.Enrolments.CountAsync());
    }

    [Fact]
    public async Task EnrolWhenCategoryIsFullReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (_, first, _, category) = await SeedAsync(db, maxParticipants: 1);
        var second = await db.AddUserAsync(UserRole.Participant);
        await db.AddEnrolmentAsync(first, category);
        var controller = new EnrolmentsController(db).WithSession(second.UserId, second.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, await db.Enrolments.CountAsync());
    }

    [Fact]
    public async Task EnrolCancelledEnrolmentsDoNotCountTowardsCapacity()
    {
        using var db = TestHelpers.CreateDb();
        var (_, first, _, category) = await SeedAsync(db, maxParticipants: 1);
        var second = await db.AddUserAsync(UserRole.Participant);
        await db.AddEnrolmentAsync(first, category, EnrolmentStatus.Cancelled);
        var controller = new EnrolmentsController(db).WithSession(second.UserId, second.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task EnrolAfterCancellingReactivatesTheSameRow()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        await db.AddEnrolmentAsync(participant, category, EnrolmentStatus.Cancelled, PaymentStatus.Refunded);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Enrol(category.CategoryId);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, await db.Enrolments.CountAsync());
        var stored = await db.Enrolments.SingleAsync();
        Assert.Equal(EnrolmentStatus.Registered, stored.Status);
        Assert.Equal(PaymentStatus.Pending, stored.PaymentStatus);
    }

    // ---------- Reading enrolments ----------

    [Fact]
    public async Task GetMineReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        using var db = TestHelpers.CreateDb();
        var (_, first, _, category) = await SeedAsync(db);
        var second = await db.AddUserAsync(UserRole.Participant);
        await db.AddEnrolmentAsync(first, category);
        await db.AddEnrolmentAsync(second, category);
        var controller = new EnrolmentsController(db).WithSession(first.UserId, first.Role);

        var result = await controller.GetMine();

        var list = Assert.IsType<List<EnrolmentResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(list);
        Assert.Equal(first.UserId, list[0].ParticipantId);
    }

    [Fact]
    public async Task GetByIdByTheOwningParticipantReturns200()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.GetById(enrolment.EnrolmentId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByIdByTheEventsOrganiserReturns200()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.GetById(enrolment.EnrolmentId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByIdByAnUnrelatedParticipantReturns403()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var stranger = await db.AddUserAsync(UserRole.Participant);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(stranger.UserId, stranger.Role);

        var result = await controller.GetById(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetByIdByAnotherEventsOrganiserReturns403()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.GetById(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetByIdUnknownIdReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, _) = await SeedAsync(db);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetForEventByOwningOrganiserReturnsTheEventsEnrolments()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, participant, ev, category) = await SeedAsync(db);
        await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.GetForEvent(ev.EventId, null);

        var list = Assert.IsType<List<EnrolmentResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetForEventByAnotherOrganiserReturns403()
    {
        using var db = TestHelpers.CreateDb();
        var (_, _, ev, _) = await SeedAsync(db);
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new EnrolmentsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.GetForEvent(ev.EventId, null);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetForEventUnknownEventReturns404()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, _, _, _) = await SeedAsync(db);
        var controller = new EnrolmentsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.GetForEvent(999, null);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task CancelByOwnerSetsStatusToCancelled()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(EnrolmentStatus.Cancelled, (await db.Enrolments.SingleAsync()).Status);
    }

    [Fact]
    public async Task CancelPaidEnrolmentIsMarkedRefunded()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category, EnrolmentStatus.Confirmed, PaymentStatus.Paid);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        await controller.Cancel(enrolment.EnrolmentId);

        Assert.Equal(PaymentStatus.Refunded, (await db.Enrolments.SingleAsync()).PaymentStatus);
    }

    [Fact]
    public async Task CancelByAnotherParticipantReturns403AndChangesNothing()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var stranger = await db.AddUserAsync(UserRole.Participant);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(stranger.UserId, stranger.Role);

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(EnrolmentStatus.Registered, (await db.Enrolments.SingleAsync()).Status);
    }

    [Fact]
    public async Task CancelAlreadyCancelledReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category, EnrolmentStatus.Cancelled);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CancelWhenAResultExistsReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category, EnrolmentStatus.Confirmed);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, participant.Role);

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(EnrolmentStatus.Confirmed, (await db.Enrolments.SingleAsync()).Status);
    }

    // ---------- Organiser updates ----------

    [Fact]
    public async Task UpdateByOwningOrganiserConfirmsAndMarksPaid()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, new EnrolmentUpdateRequest
        {
            Status = EnrolmentStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid
        });

        Assert.IsType<OkObjectResult>(result);
        var stored = await db.Enrolments.SingleAsync();
        Assert.Equal(EnrolmentStatus.Confirmed, stored.Status);
        Assert.Equal(PaymentStatus.Paid, stored.PaymentStatus);
    }

    [Fact]
    public async Task UpdateByAnotherOrganiserReturns403AndChangesNothing()
    {
        using var db = TestHelpers.CreateDb();
        var (_, participant, _, category) = await SeedAsync(db);
        var otherOrganiser = await db.AddUserAsync(UserRole.Organiser);
        var enrolment = await db.AddEnrolmentAsync(participant, category);
        var controller = new EnrolmentsController(db).WithSession(otherOrganiser.UserId, otherOrganiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, new EnrolmentUpdateRequest
        {
            Status = EnrolmentStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid
        });

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(EnrolmentStatus.Registered, (await db.Enrolments.SingleAsync()).Status);
    }

    [Fact]
    public async Task UpdateCancellingAnEnrolmentThatHasAResultReturns409()
    {
        using var db = TestHelpers.CreateDb();
        var (organiser, participant, _, category) = await SeedAsync(db);
        var enrolment = await db.AddEnrolmentAsync(participant, category, EnrolmentStatus.Confirmed);
        await db.AddResultAsync(enrolment, finishTime: new TimeSpan(0, 50, 0));
        var controller = new EnrolmentsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Update(enrolment.EnrolmentId, new EnrolmentUpdateRequest
        {
            Status = EnrolmentStatus.Cancelled,
            PaymentStatus = PaymentStatus.Refunded
        });

        Assert.IsType<ConflictObjectResult>(result);
    }
}