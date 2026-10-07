namespace CinemaShowtimesApi.Domain;

public class ShowtimeSeat
{
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
    public ShowtimeSeatStatus Status { get; set; }
    public Guid? ReservationId { get; set; }
    public DateTime? HeldUntil { get; set; }
    public int Version { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Reservation? Reservation { get; set; }

    public bool IsOccupied(DateTime utcNow) =>
        Status == ShowtimeSeatStatus.Sold || (Status == ShowtimeSeatStatus.Held && HeldUntil > utcNow);

    public void Hold(Guid reservationId, DateTime until)
    {
        Status = ShowtimeSeatStatus.Held;
        ReservationId = reservationId;
        HeldUntil = until;
    }

    public void Release()
    {
        Status = ShowtimeSeatStatus.Available;
        ReservationId = null;
        HeldUntil = null;
    }

    public void Sell()
    {
        Status = ShowtimeSeatStatus.Sold;
        HeldUntil = null;
    }
}
