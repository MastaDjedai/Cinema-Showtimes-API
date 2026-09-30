using CinemaShowtimesApi.Contracts;

namespace CinemaShowtimesApi.Services.Interfaces;

public interface IMovieService
{
    Task<IReadOnlyList<MovieResponse>> ListAsync(CancellationToken cancellationToken);
    Task<MovieResponse> CreateAsync(CreateMovieRequest request, CancellationToken cancellationToken);
}
