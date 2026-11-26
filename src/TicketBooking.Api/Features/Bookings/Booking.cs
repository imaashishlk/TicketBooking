using TicketBooking.Api.Features.Events;
using TicketBooking.Api.Features.Users;

namespace TicketBooking.Api.Features.Bookings;

public class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string Status { get; set; } = "Confirmed";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}