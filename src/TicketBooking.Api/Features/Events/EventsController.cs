using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Data;
using TicketBooking.Api.Features.Events.Contracts;

namespace TicketBooking.Api.Features.Events;

[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await db.Events.AsNoTracking().ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Events.FindAsync(id);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var item = new Event
        {
            Name = request.Name,
            TotalTickets = request.TotalTickets,
            AvailableTickets = request.TotalTickets,
            StartsAtUtc = request.StartsAtUtc
        };

        db.Events.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            item
        );
    }

}
