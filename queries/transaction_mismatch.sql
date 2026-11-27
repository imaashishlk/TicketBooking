SELECT
    e.Id,
    e.TotalTickets,
    e.AvailableTickets,
    COUNT(b.Id) AS ConfirmedBookings,
    e.TotalTickets - e.AvailableTickets
        - COUNT(b.Id) AS InventoryMismatch
FROM Events e
LEFT JOIN Bookings b
    ON e.Id = b.EventId
    AND b.Status = 'Confirmed'
WHERE e.Id = 4
GROUP BY
    e.Id,
    e.TotalTickets,
    e.AvailableTickets;