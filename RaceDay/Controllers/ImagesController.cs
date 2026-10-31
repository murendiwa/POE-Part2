using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;
using RaceDay.Services;
using System.Security.Claims;

[ApiController]
[Route("api")]
public class ImagesController(RaceDayDbContext db, BlobStorageService blobs) : ControllerBase
{
    [Authorize(Roles = "Organiser")]
    [HttpPost("events/{id:int}/banner")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var error = blobs.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (ev.OrganiserId != userId) return StatusCode(403);   // only the owner may change it

        ev.BannerUrl = await blobs.UploadAsync(file, "banners");
        await db.SaveChangesAsync();
        return Ok(new { url = ev.BannerUrl });
    }

    [Authorize]
    [HttpPost("users/me/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var error = blobs.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        user.ProfilePictureUrl = await blobs.UploadAsync(file, "profiles");
        await db.SaveChangesAsync();
        return Ok(new { url = user.ProfilePictureUrl });
    }
}