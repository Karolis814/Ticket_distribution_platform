using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Events;
using TicketPlatform.Shared.Events;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? location,
        CancellationToken ct)
    {
        var events = await _eventService.GetAllAsync(ct);

        var query = events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e =>
                Contains(e.Title, title) ||
                Contains(e.Description, title));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.StartsAt.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.StartsAt.Date <= toDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(e =>
                Contains(e.Location, location));
        }

        return Ok(query.Select(ToDto).ToList());
    }

    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetLocationSuggestions(
        [FromQuery] string input,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
        {
            return Ok(Array.Empty<string>());
        }

        var events = await _eventService.GetAllAsync(ct);

        var locations = events
            .Select(e => e.Location)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct()
            .Where(l => Contains(l, input))
            .OrderBy(l => l)
            .Take(10)
            .ToList();

        return Ok(locations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var @event = await _eventService.GetByIdAsync(id, ct);
        return @event is null ? NotFound() : Ok(ToDto(@event));
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var created = await _eventService.CreateAsync(new Event
        {
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartsAt = request.StartsAt,
            Capacity = request.Capacity
        }, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    private static EventDto ToDto(Event e) =>
        new(e.Id, e.Title, e.Description, e.Location, e.StartsAt, e.Capacity);

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
