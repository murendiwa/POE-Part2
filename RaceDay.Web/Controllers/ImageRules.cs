namespace RaceDay.Web.Controllers;

public static class ImageRules
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

    public static string? Check(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image first.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType)) return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }
}