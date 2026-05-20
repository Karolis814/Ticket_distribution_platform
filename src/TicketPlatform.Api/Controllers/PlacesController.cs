using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacesController(IPlacesService placesService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PlacePredictionDto>>> Search(
        [FromQuery] string input,
        [FromQuery] string? sessionToken = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return BadRequest("Input is required.");

        if (input.Length < 3)
            return BadRequest("Input must be at least 3 characters.");

        var results = await placesService.SearchPlacesAsync(input, sessionToken, ct);
        return Ok(results.Select(p => new PlacePredictionDto(
            p.PlaceId,
            p.MainText,
            p.SecondaryText,
            p.Description
        )).ToList());
    }
}

public record PlacePredictionDto(
    string PlaceId,
    string MainText,
    string SecondaryText,
    string? Description
);

