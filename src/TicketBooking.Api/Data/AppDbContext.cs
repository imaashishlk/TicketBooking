using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Features.Bookings;
using TicketBooking.Api.Features.Events;
using TicketBooking.Api.Features.Users;

namespace TicketBooking.Api.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();
}