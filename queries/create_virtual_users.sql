USE TicketBooking;

INSERT INTO Users (Name, Email)
SELECT
    CONCAT('Load Test User ', value),
    CONCAT('loadtest', value, '@example.com')
FROM GENERATE_SERIES(1, 100);