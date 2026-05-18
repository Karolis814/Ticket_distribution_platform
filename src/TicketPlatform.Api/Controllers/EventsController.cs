using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<EventDto>>> GetUpcomingPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] string? title = null,
        [FromQuery] string? location = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest("page ≥ 1, pageSize between 1 and 100.");

        (IReadOnlyList<Event> events, int total) =
            await eventService.GetUpcomingPagedAsync(
                page,
                pageSize,
                fromDate ?? DateTimeOffset.UtcNow,
                title,
                location,
                category,
                ct);

        return Ok(new PagedResult<EventDto>(
            events.Select(MapToEventDto).ToList(),
            page,
            pageSize,
            total));
    }

    [HttpGet("locations")]
    public async Task<ActionResult<PagedResult<string>>> GetLocationSuggestions(
        [FromQuery] string input,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 50)
            return BadRequest("page ≥ 1, pageSize between 1 and 50.");

        (IReadOnlyList<string> locations, int total) =
            await eventService.GetLocationSuggestionsAsync(input, page, pageSize, ct);

        return Ok(new PagedResult<string>(locations, page, pageSize, total));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await eventService.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var @event = await eventService.GetByIdAsync(id, ct);
        return @event is null ? NotFound() : Ok(MapToEventDto(@event));
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken ct)
    {
        if (!request.TicketTypes.Any())
            return BadRequest("At least one ticket type is required.");

        if (request.TicketTypes.Any(t => t.Quantity < 1))
            return BadRequest("Ticket type quantity must be at least 1.");

        if (request.TicketTypes.Any(t => t.PriceCents < 0))
            return BadRequest("Ticket type price cannot be negative.");

        if (request.TicketTypes.Any(t => t.MaxUses < 0))
            return BadRequest("Ticket type maximum uses cannot be negative.");

        if (request.TicketTypes.Any(t => t.OccurenceEndDate <= t.OccurenceStartDate))
            return BadRequest("Ticket type end date must be after start date.");

        if (request.TicketTypes.Any(t => t.AdmissionEndDate <= t.AdmissionStartDate))
            return BadRequest("Ticket type admission end date must be after admission start date.");

        var @event = await eventService.CreateAsync(new Event
        {
            HostId = request.HostId,
            Category = request.Category,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            ThumbnailUrl = request.ThumbnailUrl,
            Status = request.Status,
            TicketTypes = request.TicketTypes.Select(tt => new TicketType
            {
                Title = tt.Title,
                OccurenceStartDate = tt.OccurenceStartDate,
                OccurenceEndDate = tt.OccurenceEndDate,
                AdmissionStartDate = tt.AdmissionStartDate,
                AdmissionEndDate = tt.AdmissionEndDate,
                PriceCents = tt.PriceCents,
                Currency = tt.Currency,
                MaxUses = tt.MaxUses,
                Quantity = tt.Quantity
            }).ToList()
        }, ct);

        var created = await eventService.GetByIdAsync(@event.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, MapToEventDto(created));
    }

    private static EventDto MapToEventDto(Event e) => new(
        e.Id,
        e.HostId,
        e.Category,
        e.Title,
        e.Description,
        e.Location,
        e.ThumbnailUrl,
        e.Status,
        e.TicketTypes.Select(tt => new TicketTypeDto(
            tt.Id,
            tt.EventId,
            tt.Title,
            tt.OccurenceStartDate,
            tt.OccurenceEndDate,
            tt.AdmissionStartDate,
            tt.AdmissionEndDate,
            tt.PriceCents,
            tt.Currency,
            tt.MaxUses,
            tt.Quantity,
            tt.Tickets.Count
        )).ToList()
    );
}
