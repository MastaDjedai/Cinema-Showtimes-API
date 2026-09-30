using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Repositories;

public sealed class ReservationRepository(CinemaDbContext db) : IReservationRepository
{
    public Task<Showtime?> GetShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken)
    {
        return db.Showtimes
            .Include(x => x.Movie)
            .Include(x => x.Auditorium).ThenInclude(x => x.Seats)
            .Include(x => x.Reservations).ThenInclude(x => x.Seats).ThenInclude(x => x.Seat)
            .FirstOrDefaultAsync(x => x.Id == showtimeId, cancellationToken);
    }

    public Task<Reservation?> GetByIdForConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        return db.Reservations
            .Include(x => x.Seats).ThenInclude(x => x.Seat)
            .Include(x => x.Showtime).ThenInclude(x => x.Movie)
            .Include(x => x.Showtime).ThenInclude(x => x.Auditorium)
            .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
    }

    public async Task ExpirePendingAsync(Guid showtimeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var expired = await db.Reservations
            .Where(x => x.ShowtimeId == showtimeId && x.Status == ReservationStatus.Pending && x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var reservation in expired)
        {
            reservation.Status = ReservationStatus.Expired;
        }

        if (expired.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
