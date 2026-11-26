using System.ComponentModel.DataAnnotations;

namespace TicketBooking.Api.Features.Events.Contracts;

public record CreateEventRequest(
    [Required] string Name,
    [Range(1, int.MaxValue)] int TotalTickets,
    DateTime StartsAtUtc
);
