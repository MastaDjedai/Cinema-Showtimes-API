namespace CinemaShowtimesApi.Domain;

public class Showtime
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid AuditoriumId { get; set; }
    public DateTimeOffset StartTime { get; set; }

    public Movie Movie { get; set; } = null!;
    public Auditorium Auditorium { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
