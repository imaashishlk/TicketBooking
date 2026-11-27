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
        var userExists = await db.Users.AnyAsync(
            u => u.Id == request.UserId,
            cancellationToken);

        if (!userExists)
            return NotFound("User not found.");

        // Begin transaction, and dispose if not complete
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var item = await db.Events
            .FromSqlInterpolated($"""
                        SELECT *
                        FROM Events WITH (UPDLOCK, ROWLOCK)
                        WHERE Id = {request.EventId}
                        """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
            return NotFound("Event not found.");

        if (item.AvailableTickets <= 0)
            return Conflict("Tickets are sold out.");

        item.AvailableTickets--;

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

        return StatusCode(201, new
        {
            booking.Id,
            booking.UserId,
            booking.EventId,
            booking.Status,
            booking.CreatedAtUtc
        });
    }
}
