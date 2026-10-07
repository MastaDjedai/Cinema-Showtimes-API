using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    Task<Showtime?> GetShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken);
    Task<Reservation?> GetByIdForConfirmAsync(Guid reservationId, CancellationToken cancellationToken);
    Task ExpirePendingHoldsAsync(Guid showtimeId, DateTime utcNow, CancellationToken cancellationToken);
    void Add(Reservation reservation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
