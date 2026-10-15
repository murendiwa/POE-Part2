using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Controllers;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Test;

public class EventsControllerTests
{
    private static EventRequest ValidRequest(DateTime? date = null) => new()
    {
        Name = "Test Fun Run",
        Description = "Testing",
        EventDate = date ?? DateTime.Now.AddDays(30),
        Location = "Pretoria",
        Province = "Gauteng",
        Status = EventStatus.Scheduled
    };

    // ---------- Public reads ----------

    [Fact]
    public async Task GetAll_WithoutLogin_ReturnsAllEvents()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        await db.AddEventAsync(organiser, "Event A", "Gauteng");
        await db.AddEventAsync(organiser, "Event B", "Western Cape");
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetAll(null, null);

        var list = Assert.IsType<List<EventResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByProvince()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        await db.AddEventAsync(organiser, "Event A", "Gauteng");
        await db.AddEventAsync(organiser, "Event B", "Western Cape");
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetAll("Gauteng", null);

        var list = Assert.IsType<List<EventResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(list);
        Assert.Equal("Event A", list[0].Name);
    }

    [Fact]
    public async Task GetById_ReturnsEventWithItsCategories()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(organiser);
        await db.AddCategoryAsync(ev, "10km");
        await db.AddCategoryAsync(ev, "5km");
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetById(ev.EventId);

        var detail = Assert.IsType<EventDetailResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, detail.Categories.Count);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_AsOrganiser_AssignsTheLoggedInOrganiserAsOwner()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(ValidRequest());

        Assert.IsType<CreatedAtActionResult>(result);
        var stored = await db.Events.SingleAsync();
        Assert.Equal(organiser.UserId, stored.OrganiserId);
    }

    [Fact]
    public async Task Create_WithPastDate_ReturnsBadRequestAndSavesNothing()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Create(ValidRequest(DateTime.Now.AddDays(-1)));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.Events.CountAsync());
    }

    // ---------- Update (ownership) ----------

    [Fact]
    public async Task Update_ByOwner_ChangesTheEvent()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(organiser, "Old Name");
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var request = ValidRequest();
        request.Name = "New Name";
        var result = await controller.Update(ev.EventId, request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("New Name", (await db.Events.SingleAsync()).Name);
    }

    [Fact]
    public async Task Update_ByDifferentOrganiser_Returns403AndDoesNotChangeTheEvent()
    {
        using var db = TestHelpers.CreateDb();
        var owner = await db.AddUserAsync(UserRole.Organiser);
        var intruder = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(owner, "Original");
        var controller = new EventsController(db).WithSession(intruder.UserId, intruder.Role);

        var request = ValidRequest();
        request.Name = "Hijacked";
        var result = await controller.Update(ev.EventId, request);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal("Original", (await db.Events.SingleAsync()).Name);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Update(999, ValidRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- Delete (ownership + business rule) ----------

    [Fact]
    public async Task Delete_ByDifferentOrganiser_Returns403AndKeepsTheEvent()
    {
        using var db = TestHelpers.CreateDb();
        var owner = await db.AddUserAsync(UserRole.Organiser);
        var intruder = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(owner);
        var controller = new EventsController(db).WithSession(intruder.UserId, intruder.Role);

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(1, await db.Events.CountAsync());
    }

    [Fact]
    public async Task Delete_WhenSomeoneIsEnrolled_Returns409AndKeepsTheEvent()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var participant = await db.AddUserAsync(UserRole.Participant);
        var ev = await db.AddEventAsync(organiser);
        var category = await db.AddCategoryAsync(ev);
        await db.AddEnrolmentAsync(participant, category);
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, await db.Events.CountAsync());
    }

    [Fact]
    public async Task Delete_ByOwnerWithNoEnrolments_Returns204AndRemovesTheEvent()
    {
        using var db = TestHelpers.CreateDb();
        var organiser = await db.AddUserAsync(UserRole.Organiser);
        var ev = await db.AddEventAsync(organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, organiser.Role);

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await db.Events.CountAsync());
    }
}