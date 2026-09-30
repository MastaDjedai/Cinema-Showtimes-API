using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Repositories.Interfaces;

public interface IMovieRepository
{
    Task<IReadOnlyList<Movie>> ListOrderedByTitleAsync(CancellationToken cancellationToken);

    Task AddAsync(Movie movie, CancellationToken cancellationToken);
}
