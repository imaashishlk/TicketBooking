using System.ComponentModel.DataAnnotations;

namespace TicketBooking.Api.Features.Bookings.Contracts;

public record CreateBookingRequest(
    [Range(1, int.MaxValue)] int UserId,
    [Range(1, int.MaxValue)] int EventId
);
