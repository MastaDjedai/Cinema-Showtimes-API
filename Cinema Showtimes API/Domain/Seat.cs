namespace CinemaShowtimesApi.Domain;

public class Seat
{
    public Guid Id { get; set; }
    public Guid AuditoriumId { get; set; }
    public required string Row { get; set; }
    public int Number { get; set; }

    public Auditorium Auditorium { get; set; } = null!;
    public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
}
