using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Models;
using System.Diagnostics.CodeAnalysis;

namespace RaceDay.Test;

/// <summary>Minimal in-memory stand-in for the server-side session.</summary>
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class TestHelpers
{
    /// <summary>A fresh, empty in-memory database for each test.</summary>
    public static RaceDayDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayDbContext(options);
    }

    /// <summary>Gives the controller a session. Pass a user to simulate a logged-in request; pass nothing for an anonymous one.</summary>
    public static T WithSession<T>(this T controller, int? userId = null, UserRole? role = null) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new FakeSession() };
        if (userId is not null && role is not null)
            http.Session.SetUser(userId.Value, role.Value);

        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    public static async Task<User> AddUserAsync(this RaceDayDbContext db, UserRole role,
        string? name = null, string? email = null, string password = "Password123!")
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var user = new User
        {
            FullName = name ?? $"{role} {tag}",
            Email = email ?? $"{tag}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4), // low work factor keeps tests fast
            Role = role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Event> AddEventAsync(this RaceDayDbContext db, User organiser,
        string name = "Test Event", string province = "Gauteng",
        EventStatus status = EventStatus.Scheduled, DateTime? date = null)
    {
        var ev = new Event
        {
            OrganiserId = organiser.UserId,
            Name = name,
            Location = "Pretoria",
            Province = province,
            Status = status,
            EventDate = date ?? DateTime.Now.AddDays(30)
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    public static async Task<Category> AddCategoryAsync(this RaceDayDbContext db, Event ev,
        string name = "10km", int maxParticipants = 100)
    {
        var category = new Category
        {
            EventId = ev.EventId,
            Name = name,
            DistanceKm = 10,
            MaxParticipants = maxParticipants,
            EntryFee = 150
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public static async Task<Enrolment> AddEnrolmentAsync(this RaceDayDbContext db, User participant, Category category,
        EnrolmentStatus status = EnrolmentStatus.Registered, PaymentStatus payment = PaymentStatus.Pending)
    {
        var enrolment = new Enrolment
        {
            ParticipantId = participant.UserId,
            CategoryId = category.CategoryId,
            Status = status,
            PaymentStatus = payment
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        return enrolment;
    }
    public static async Task<Result> AddResultAsync(this RaceDayDbContext db, Enrolment enrolment,
    ResultStatus status = ResultStatus.Finished, int? position = 1, TimeSpan? finishTime = null)
    {
        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            Status = status,
            Position = position,
            FinishTime = (TimeSpan)finishTime
        };
        db.Results.Add(result);
        await db.SaveChangesAsync();
        return result;
    }
}
