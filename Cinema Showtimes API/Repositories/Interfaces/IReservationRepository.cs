using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<Showtime?> GetShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken);

    Task<Reservation?> GetByIdForConfirmAsync(Guid reservationId, CancellationToken cancellationToken);

    Task ExpirePendingAsync(Guid showtimeId, DateTime utcNow, CancellationToken cancellationToken);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetClaimedSeatIdsAsync(Guid showtimeId, CancellationToken cancellationToken);

    Task AddWithClaimsAsync(Reservation reservation, CancellationToken cancellationToken);

    Task<Reservation> CreateReservationIfAvailableAsync(Reservation reservation, DateTime utcNow, CancellationToken cancellationToken);
}
