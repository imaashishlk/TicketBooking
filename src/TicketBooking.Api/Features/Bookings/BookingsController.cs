using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Data;
using TicketBooking.Api.Features.Bookings.Contracts;

namespace TicketBooking.Api.Features.Bookings;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db) : ControllerBase
{
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
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists) return NotFound("User not found");

        // event is reserved keyword, so using @event to avoid naming conflict
        var @event = await db.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!@event) return NotFound("Event not found");

        // Begin transaction (explicitly) to ensure that the booking creation and ticket availability update are atomic.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);


        var rowsAffected = await db.Events
                        .Where(e => e.Id == request.EventId && e.AvailableTickets > 0)
                        .ExecuteUpdateAsync(e => e.SetProperty(ev => ev.AvailableTickets,
                            ev => ev.AvailableTickets - 1), cancellationToken);

        if (rowsAffected == 0) return Conflict("No tickets available for this event");

        var booking = new Booking
        {
            UserId = request.UserId,
            EventId = request.EventId,
            Status = "Confirmed",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Bookings.Add(booking);

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }
}
