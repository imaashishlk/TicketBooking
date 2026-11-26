namespace TicketBooking.Api.Features.Events;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime StartsAtUtc { get; set; }
}