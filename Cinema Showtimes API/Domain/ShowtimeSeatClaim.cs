namespace CinemaShowtimesApi.Domain;

public class ShowtimeSeatClaim
{
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
    public Guid ReservationId { get; set; }
}
