using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;
using TicketPlatform.Shared.Dtos;

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
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetAll(CancellationToken ct)
    {
        var events = await _eventService.GetAllAsync(ct);
        return Ok(events.Select(ToDto).ToList());
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
            TicketCount = request.TicketCount,
        }, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    private static EventDto ToDto(Event e) =>
        new(e.Id, e.Title, e.Description, e.Location, e.StartsAt,e.EndsAt, e.TicketCount,e.HostId);
}
