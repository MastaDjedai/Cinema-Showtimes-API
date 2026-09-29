using System.ComponentModel.DataAnnotations;

namespace CinemaShowtimesApi.Contracts;

public sealed class CreateShowtimeRequest
{
    [Required]
    public Guid MovieId { get; set; }

    [Required]
    public Guid AuditoriumId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }
}

public sealed class ShowtimeResponse
{
    public required Guid Id { get; set; }
    public required Guid MovieId { get; set; }
    public required string MovieTitle { get; set; }
    public required Guid AuditoriumId { get; set; }
    public required string AuditoriumName { get; set; }
    public required DateTimeOffset StartTime { get; set; }
}

public sealed class AuditoriumResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyList<SeatDto> Seats { get; set; }
}

public sealed class SeatDto
{
    [Required]
    [RegularExpression(@"^[A-Za-z]$", ErrorMessage = "Row must be a single letter.")]
    public required string Row { get; set; }

    [Range(1, 50)]
    public int Number { get; set; }
}
