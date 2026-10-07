using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Repositories;

public sealed class ReservationRepository(CinemaDbContext db) : IReservationRepository
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (AppException)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task<Showtime?> GetShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken)
    {
        return db.Showtimes
            .Include(x => x.Movie)
            .Include(x => x.Auditorium).ThenInclude(x => x.Seats)
            .Include(x => x.Occupancies).ThenInclude(x => x.Seat)
            .FirstOrDefaultAsync(x => x.Id == showtimeId, cancellationToken);
    }

    public Task<Reservation?> GetByIdForConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        return db.Reservations
            .Include(x => x.Seats).ThenInclude(x => x.Seat)
            .Include(x => x.Showtime).ThenInclude(x => x.Movie)
            .Include(x => x.Showtime).ThenInclude(x => x.Auditorium)
            .Include(x => x.Showtime).ThenInclude(x => x.Occupancies)
            .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
    }

    public async Task ExpirePendingHoldsAsync(Guid showtimeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var expiredReservations = await db.Reservations
            .Where(x => x.ShowtimeId == showtimeId && x.Status == ReservationStatus.Pending && x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var reservation in expiredReservations)
        {
            reservation.Status = ReservationStatus.Expired;
        }

        var expiredIds = expiredReservations.Select(x => x.Id).ToHashSet();
        var occupancies = await db.ShowtimeSeats
            .Where(x => x.ShowtimeId == showtimeId && x.Status == ShowtimeSeatStatus.Held)
            .ToListAsync(cancellationToken);

        foreach (var occupancy in occupancies)
        {
            var holdExpired = occupancy.HeldUntil is null || occupancy.HeldUntil <= utcNow;
            var reservationExpired = occupancy.ReservationId is { } reservationId && expiredIds.Contains(reservationId);
            if (holdExpired || reservationExpired)
            {
                occupancy.Release();
            }
        }
    }

    public void Add(Reservation reservation) => db.Reservations.Add(reservation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
