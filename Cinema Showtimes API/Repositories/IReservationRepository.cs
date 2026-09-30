using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Repositories;

public interface IReservationRepository
{
    Task<Showtime?> GetShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken);
    Task<Reservation?> GetByIdForConfirmAsync(Guid reservationId, CancellationToken cancellationToken);
    Task ExpirePendingAsync(Guid showtimeId, DateTime utcNow, CancellationToken cancellationToken);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
