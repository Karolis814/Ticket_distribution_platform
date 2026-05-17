using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class ImagesClient(HttpClient http) : IImagesClient
{
    public async Task<UploadImageResponse> UploadEventThumbnailAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        var response = await http.PostAsync("api/images/event-thumbnails", form, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Server returned no payload.");
    }
}
