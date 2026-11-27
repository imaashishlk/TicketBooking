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
    public async Task<IActionResult> Create(
    CreateBookingRequest request,
    CancellationToken cancellationToken)
    {
        var userExists = await db.Users.AnyAsync(
            u => u.Id == request.UserId,
            cancellationToken);

        if (!userExists)
            return NotFound("User not found.");

        const int maxAttempts = 20;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var item = await db.Events.FindAsync(
                new object[] { request.EventId },
                cancellationToken);

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

            try
            {
                await db.SaveChangesAsync(cancellationToken);

                return StatusCode(201, new
                {
                    booking.Id,
                    booking.UserId,
                    booking.EventId,
                    booking.Status,
                    booking.CreatedAtUtc
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();

                if (attempt == maxAttempts)
                {
                    return StatusCode(
                        503,
                        "Booking temporarily unavailable. Please retry.");
                }

                await Task.Delay(
                    Random.Shared.Next(10, 50),
                    cancellationToken);
            }
        }

        return StatusCode(503, "Booking temporarily unavailable.");
    }
}
