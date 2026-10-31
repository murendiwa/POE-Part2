using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Services;

public class BlobStorageService(BlobServiceClient client, IConfiguration config)
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] AllowedExt = [".jpg", ".jpeg", ".png", ".webp"];

    public string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext) || !AllowedTypes.Contains(file.ContentType))
            return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var container = client.GetBlobContainerClient(config["Azure:Container"] ?? "raceday-images");
        var name = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var blob = container.GetBlobClient(name);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}
