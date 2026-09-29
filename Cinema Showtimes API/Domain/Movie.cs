namespace CinemaShowtimesApi.Domain;

public class Movie
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; }
    public int Year { get; set; }
    public int DurationMinutes { get; set; }

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
