using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Data;
using TicketBooking.Api.Features.Bookings.Contracts;

namespace TicketBooking.Api.Features.Bookings;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db) : ControllerBase
{
    // SemaphoreSlim is used to ensure that only one thread can access the critical section of code at a time,
    // preventing race conditions when multiple requests are made simultaneously.
    private static readonly SemaphoreSlim _gate = new(1,1);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await db.Bookings.AsNoTracking().ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Bookings.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId);

        if (!userExists)
            return NotFound("User not found");

        await _gate.WaitAsync(cancellationToken);

        try
        {
            // event is reserved keyword, so using @event to avoid naming conflict
            var @event = await db.Events.FindAsync(request.EventId);

            if (@event is null)
                return NotFound("Event not found");

            if (@event.AvailableTickets <= 0)
            {
                return Conflict("No tickets available for this event");
            }

            @event.AvailableTickets--;

            var booking = new Booking
            {
                UserId = request.UserId,
                EventId = request.EventId,
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
        }
        finally
        {
            _gate.Release();
        }
    }
}
