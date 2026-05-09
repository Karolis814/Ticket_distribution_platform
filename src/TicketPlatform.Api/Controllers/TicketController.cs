using Microsoft.AspNetCore.Mvc;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _service;

    public TicketsController(TicketService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
            return NotFound();

        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Ticket entity)
    {
        var created = await _service.CreateAsync(entity);

        return Ok(created);
    }
}
