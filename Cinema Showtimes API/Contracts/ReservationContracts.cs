using System.ComponentModel.DataAnnotations;

namespace CinemaShowtimesApi.Contracts;

public sealed class ReserveSeatsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one seat is required.")]
    [MaxLength(20, ErrorMessage = "A reservation can include at most 20 seats.")]
    public required IReadOnlyList<SeatDto> Seats { get; set; }
}

public sealed class ReserveContiguousSeatsRequest
{
    [Range(1, 10)]
    public int SeatCount { get; set; }
}

public sealed class ReservationResponse
{
    public required Guid ReservationReference { get; set; }
    public required int SeatCount { get; set; }
    public required string Auditorium { get; set; }
    public required string Movie { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required IReadOnlyList<SeatDto> Seats { get; set; }
}
