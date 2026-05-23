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
        ["image/jpeg", "image/png", "image/webp"];

    private static readonly Dictionary<string, string> ExtensionByContentType = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

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
            return BadRequest($"Unsupported content type. Allowed: {string.Join(", ", AllowedContentTypes)}.");

        var extension = ExtensionByContentType[contentType];
        var blobName = $"{Guid.NewGuid():N}{extension}";

        await using var stream = file.OpenReadStream();
        var uri = await blobStorage.UploadAsync(
            _opts.ThumbnailsContainer,
            blobName,
            stream,
            contentType,
            ct);

        return Ok(new UploadImageResponse(uri.ToString()));
    }
}
