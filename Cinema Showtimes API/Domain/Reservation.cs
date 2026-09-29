namespace CinemaShowtimesApi.Domain;

public class Reservation
{
    public const int DefaultTtlMinutes = 10;

    public Guid Id { get; set; }
    public Guid ShowtimeId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public ICollection<ReservationSeat> Seats { get; set; } = new List<ReservationSeat>();

    public bool IsPending(DateTime utcNow) =>
        Status == ReservationStatus.Pending && ExpiresAt > utcNow;
}
