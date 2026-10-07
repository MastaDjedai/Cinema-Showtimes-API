using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Repositories;

public sealed class ShowtimeRepository(CinemaDbContext db) : IShowtimeRepository
{
    public async Task<IReadOnlyList<Auditorium>> ListAuditoriumsWithSeatsAsync(CancellationToken cancellationToken)
    {
        return await db.Auditoriums.AsNoTracking().Include(x => x.Seats).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Movie?> GetMovieByIdAsync(Guid movieId, CancellationToken cancellationToken)
    {
        return db.Movies.FirstOrDefaultAsync(x => x.Id == movieId, cancellationToken);
    }

    public Task<Auditorium?> GetAuditoriumByIdAsync(Guid auditoriumId, CancellationToken cancellationToken)
    {
        return db.Auditoriums.Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == auditoriumId, cancellationToken);
    }

    public Task<bool> HasOverlappingShowtimeAsync(Guid auditoriumId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
    {
        return db.Showtimes.AnyAsync(x => x.AuditoriumId == auditoriumId && x.StartTime < endTime && startTime < x.EndTime, cancellationToken);
    }

    public async Task AddAsync(Showtime showtime, CancellationToken cancellationToken)
    {
        var seats = await db.Seats.Where(x => x.AuditoriumId == showtime.AuditoriumId).Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var seatId in seats)
        {
            showtime.Occupancies.Add(new ShowtimeSeat { ShowtimeId = showtime.Id, SeatId = seatId, Status = ShowtimeSeatStatus.Available });
        }

        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync(cancellationToken);
    }
}
