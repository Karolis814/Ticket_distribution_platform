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

    [HttpGet("details")]
    public async Task<ActionResult<PlaceDetailsDto>> Details(
        [FromQuery] string placeId,
        [FromQuery] string? sessionToken = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(placeId))
            return BadRequest("placeId is required.");

        var details = await placesService.GetPlaceDetailsAsync(placeId, sessionToken, ct);
        if (details is null)
            return NotFound();

        return Ok(new PlaceDetailsDto(
            details.PlaceId,
            details.Name,
            details.StreetAddress,
            details.PostalCode,
            details.City,
            details.Country,
            details.FormattedAddress
        ));
    }
}

public record PlacePredictionDto(
    string PlaceId,
    string MainText,
    string SecondaryText,
    string? Description
);

public record PlaceDetailsDto(
    string PlaceId,
    string? Name,
    string? StreetAddress,
    string? PostalCode,
    string? City,
    string? Country,
    string? FormattedAddress
);
