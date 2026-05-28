using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketPlatform.Core.Services;
using TicketPlatform.Infrastructure.Storage;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(
    IBlobStorageService blobStorage,
    IOptions<BlobStorageOptions> opts) : ControllerBase
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"];

    private readonly BlobStorageOptions _opts = opts.Value;

    [HttpPost("event-thumbnails")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadImageResponse>> UploadEventThumbnail(
        IFormFile file,
        CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        if (file.Length > _opts.MaxImageBytes)
            return BadRequest($"File exceeds {_opts.MaxImageBytes / (1024 * 1024)} MB limit.");

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
            return BadRequest($"Unsupported content type. Allowed: JPEG, PNG, WebP, HEIC.");

        var blobName = $"{Guid.NewGuid():N}.jpg";

        using var image = new MagickImage(file.OpenReadStream());
        image.AutoOrient();
        image.Resize(new MagickGeometry("1280x720^"));
        image.Crop(1280, 720, Gravity.Center);
        image.ResetPage();
        image.Format = MagickFormat.Jpeg;
        image.Quality = 92;

        using var output = new MemoryStream();
        await image.WriteAsync(output, ct);
        output.Position = 0;

        var uri = await blobStorage.UploadAsync(
            _opts.ThumbnailsContainer,
            blobName,
            output,
            "image/jpeg",
            ct);

        return Ok(new UploadImageResponse(uri.ToString()));
    }
}
