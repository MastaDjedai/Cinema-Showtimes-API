using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
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
        return db.Auditoriums.FirstOrDefaultAsync(x => x.Id == auditoriumId, cancellationToken);
    }

    public Task<bool> HasShowtimeAtAsync(Guid auditoriumId, DateTimeOffset startTime, CancellationToken cancellationToken)
    {
        return db.Showtimes.AnyAsync(x => x.AuditoriumId == auditoriumId && x.StartTime == startTime, cancellationToken);
    }

    public async Task AddAsync(Showtime showtime, CancellationToken cancellationToken)
    {
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync(cancellationToken);
    }
}
