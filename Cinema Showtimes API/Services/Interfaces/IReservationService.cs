using CinemaShowtimesApi.Contracts;

namespace CinemaShowtimesApi.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationResponse> ReserveAsync(Guid showtimeId, IReadOnlyList<SeatDto> requestedSeats, CancellationToken cancellationToken);

    Task<ReservationResponse> ReserveContiguousAsync(Guid showtimeId, int seatCount, CancellationToken cancellationToken);

    Task<ReservationResponse> ConfirmAsync(Guid reservationReference, CancellationToken cancellationToken);
}
