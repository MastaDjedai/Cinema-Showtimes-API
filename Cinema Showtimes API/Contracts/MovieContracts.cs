using System.ComponentModel.DataAnnotations;

namespace CinemaShowtimesApi.Contracts;

public sealed class CreateMovieRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; set; }

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public required string Category { get; set; }

    [Range(1888, 2100)]
    public int Year { get; set; }

    [Range(1, 600)]
    public int DurationMinutes { get; set; } = 120;
}

public sealed class MovieResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; }
    public required int Year { get; set; }
    public required int DurationMinutes { get; set; }
}
