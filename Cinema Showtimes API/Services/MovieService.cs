using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Repositories;
using CinemaShowtimesApi.Services.Interfaces;

namespace CinemaShowtimesApi.Services;

public sealed class MovieService(IMovieRepository movieRepository) : IMovieService
{
    public async Task<IReadOnlyList<MovieResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await movieRepository.ListOrderedByTitleAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
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

        await movieRepository.AddAsync(movie, cancellationToken);

        return ToResponse(movie);
    }

    private MovieResponse ToResponse(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Category = movie.Category,
        Year = movie.Year,
        DurationMinutes = movie.DurationMinutes
    };
}
