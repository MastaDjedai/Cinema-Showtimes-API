using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Repositories;

public sealed class MovieRepository(CinemaDbContext db) : IMovieRepository
{
    public async Task<IReadOnlyList<Movie>> ListOrderedByTitleAsync(CancellationToken cancellationToken)
    {
        return await db.Movies.AsNoTracking().OrderBy(x => x.Title).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Movie movie, CancellationToken cancellationToken)
    {
        db.Movies.Add(movie);
        await db.SaveChangesAsync(cancellationToken);
    }
}
