namespace CinemaShowtimesApi.Domain;

public class Auditorium
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
