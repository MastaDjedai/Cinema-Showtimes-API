using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
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

        if (expired.Count == 0) return;

        var expiredIds = expired.Select(x => x.Id).ToHashSet();

        foreach (var reservation in expired)
        {
            reservation.Status = ReservationStatus.Expired;
        }

        var claims = await db.ShowtimeSeatClaims.Where(claims => claims.ShowtimeId == showtimeId && expiredIds.Contains(claims.ReservationId)).ToListAsync(cancellationToken);

        db.RemoveRange(claims, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<HashSet<Guid>> GetClaimedSeatIdsAsync(Guid showtimeId, CancellationToken cancellationToken)
    {
        return db.ShowtimeSeatClaims.Where(claims => claims.ShowtimeId == showtimeId).Select(claims => claims.SeatId).ToHashSetAsync(cancellationToken);
    }

    public async Task AddWithClaimsAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        db.Reservations.Add(reservation);

        foreach (var seat in reservation.Seats)
        {
            db.ShowtimeSeatClaims.Add(new ShowtimeSeatClaim
            {
                ReservationId = reservation.Id,
                ShowtimeId = reservation.ShowtimeId,
                SeatId = seat.SeatId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Reservation> CreateReservationIfAvailableAsync(Reservation reservation, DateTime utcNow, CancellationToken cancellationToken)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = connection.BeginTransaction(deferred: false);

        await db.Database.UseTransactionAsync(transaction, cancellationToken);

        var expired = await db.Reservations.Where(x => x.ShowtimeId == reservation.ShowtimeId && x.Status == ReservationStatus.Pending && x.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var item in expired)
        {
            item.Status = ReservationStatus.Expired;
        }

        var expiredIds = expired.Select(x => x.Id).ToHashSet();

        if (expiredIds.Count > 0)
        {
            var expiredClaims = await db.ShowtimeSeatClaims.Where(x => x.ShowtimeId == reservation.ShowtimeId && expiredIds.Contains(x.ReservationId)).ToListAsync(cancellationToken);

            db.ShowtimeSeatClaims.RemoveRange(expiredClaims);
        }

        await db.SaveChangesAsync(cancellationToken);

        var requestedSeatIds = reservation.Seats.Select(x => x.SeatId).ToHashSet();

        var unavailableSeatIds = await db.ShowtimeSeatClaims.Where(x => x.ShowtimeId == reservation.ShowtimeId && requestedSeatIds.Contains(x.SeatId))
            .Select(x => x.SeatId)
            .ToListAsync(cancellationToken);

        if (unavailableSeatIds.Count > 0)
        {
            throw new ConflictException("One or more requested seats are already reserved or sold.", "seats_unavailable");
        }

        try
        {
            db.Reservations.Add(reservation);

            foreach (var seat in reservation.Seats)
            {
                db.ShowtimeSeatClaims.Add(new ShowtimeSeatClaim
                {
                    ShowtimeId = reservation.ShowtimeId,
                    SeatId = seat.SeatId,
                    ReservationId = reservation.Id
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
        {
            throw new ConflictException("One or more requested seats are already reserved or sold.", "seats_unavailable");
        }

        return reservation;
    }
}
