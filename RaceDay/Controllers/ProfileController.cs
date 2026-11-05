using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.DTOs;
using RaceDay.Models;
using System.Security.Claims;

namespace RaceDay.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public class ProfileController(AppDbContext db) : ControllerBase   // TODO-ADAPT
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var user = await FindUserAsync();
        return user is null ? NotFound() : ToResponse(user);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(ProfileUpdateRequest req)
    {
        var user = await FindUserAsync();
        if (user is null) return NotFound();

        if (req.DateOfBirth is { } dob && dob > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date of birth",
                Detail = "The date of birth can't be in the future."
            });

        user.FullName = req.FullName.Trim();
        user.PhoneNumber = req.PhoneNumber;
        user.DateOfBirth = req.DateOfBirth;   // TODO-ADAPT: match your property type
        await db.SaveChangesAsync();

        return ToResponse(user);
    }

    private async Task<User?> FindUserAsync()   // TODO-ADAPT: your user entity name
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);   // TODO-ADAPT key name
    }

    private static ProfileResponse ToResponse(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        DateOfBirth = u.DateOfBirth,
        ProfilePictureUrl = u.ProfilePictureUrl,
        Role = u.Role
    };
}