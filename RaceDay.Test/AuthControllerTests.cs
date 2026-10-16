using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Controllers;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Test;

public class AuthControllerTests
{
    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public async Task RegisterStoresChosenRoleAndNormalisedEmail(UserRole role)
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Test User",
            Email = "Test@Example.com",
            Password = "Password123!",
            Role = role
        });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
        var stored = await db.Users.SingleAsync();
        Assert.Equal(role, stored.Role);
        Assert.Equal("test@example.com", stored.Email);
    }

    [Fact]
    public async Task RegisterHashesThePassword()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db).WithSession();

        await controller.Register(new RegisterRequest
        {
            FullName = "Test User",
            Email = "hash@test.com",
            Password = "Password123!",
            Role = UserRole.Participant
        });

        var stored = await db.Users.SingleAsync();
        Assert.NotEqual("Password123!", stored.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", stored.PasswordHash));
    }

    [Fact]
    public async Task RegisterWithExistingEmailReturnsConflict()
    {
        using var db = TestHelpers.CreateDb();
        await db.AddUserAsync(UserRole.Participant, email: "taken@test.com");
        var controller = new AuthController(db).WithSession();

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Other",
            Email = "TAKEN@test.com",
            Password = "Password123!",
            Role = UserRole.Participant
        });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task LoginWithValidCredentialsStoresUserIdAndRoleInSession()
    {
        using var db = TestHelpers.CreateDb();
        var user = await db.AddUserAsync(UserRole.Organiser, email: "org@test.com", password: "Password123!");
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginRequest { Email = "org@test.com", Password = "Password123!" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(user.UserId, controller.HttpContext.Session.GetUserId());
        Assert.Equal(UserRole.Organiser, controller.HttpContext.Session.GetRole());
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturns401AndNoSession()
    {
        using var db = TestHelpers.CreateDb();
        await db.AddUserAsync(UserRole.Participant, email: "p@test.com", password: "Password123!");
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginRequest { Email = "p@test.com", Password = "WrongPassword" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }

    [Fact]
    public async Task LoginWithUnknownEmailReturns401()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = "Password123!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ClearsTheSession()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db).WithSession(1, UserRole.Participant);

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetUserId());
        Assert.Null(controller.HttpContext.Session.GetRole());
        await Task.CompletedTask;
    }

    [Fact]
    public async Task MeReturnsTheLoggedInUser()
    {
        using var db = TestHelpers.CreateDb();
        var user = await db.AddUserAsync(UserRole.Participant, name: "Naledi", email: "naledi@test.com");
        var controller = new AuthController(db).WithSession(user.UserId, user.Role);

        var result = await controller.Me();

        var body = Assert.IsType<UserResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("naledi@test.com", body.Email);
        Assert.Equal(UserRole.Participant, body.Role);
    }
}
