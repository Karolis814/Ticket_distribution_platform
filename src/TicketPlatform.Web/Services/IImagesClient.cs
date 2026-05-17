using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public interface IImagesClient
{
    Task<UploadImageResponse> UploadEventThumbnailAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);
}
