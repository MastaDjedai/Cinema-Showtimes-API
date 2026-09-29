namespace CinemaShowtimesApi.Domain;

public class ReservationSeat
{
    public Guid ReservationId { get; set; }
    public Guid SeatId { get; set; }

    public Reservation Reservation { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
