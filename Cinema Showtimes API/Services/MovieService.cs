using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Services;

public sealed class MovieService(CinemaDbContext db)
{
    public async Task<IReadOnlyList<MovieResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Movies
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<MovieResponse> CreateAsync(CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Category = request.Category.Trim(),
            Year = request.Year,
            DurationMinutes = request.DurationMinutes
        };

        db.Movies.Add(movie);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(movie);
    }

    private static MovieResponse ToResponse(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Category = movie.Category,
        Year = movie.Year,
        DurationMinutes = movie.DurationMinutes
    };
}
