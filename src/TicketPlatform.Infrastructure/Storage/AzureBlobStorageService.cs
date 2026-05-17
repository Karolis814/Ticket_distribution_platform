using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Infrastructure.Storage;

public class AzureBlobStorageService(
    BlobServiceClient client,
    IOptions<BlobStorageOptions> opts) : IBlobStorageService
{
    private readonly BlobStorageOptions _opts = opts.Value;

    public async Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var container = client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        return blob.Uri;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var container = client.GetBlobContainerClient(containerName);
        await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
    }
}

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
    public string ThumbnailsContainer { get; set; } = "event-thumbnails";
    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
