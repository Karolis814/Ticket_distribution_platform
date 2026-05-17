namespace TicketPlatform.Core.Services;

public interface IBlobStorageService
{
    Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default);
}
