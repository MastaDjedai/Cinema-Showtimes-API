using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Repositories;

public interface IShowtimeRepository
{
    Task<IReadOnlyList<Auditorium>> ListAuditoriumsWithSeatsAsync(CancellationToken cancellationToken);
    Task<Movie?> GetMovieByIdAsync(Guid movieId, CancellationToken cancellationToken);
    Task<Auditorium?> GetAuditoriumByIdAsync(Guid auditoriumId, CancellationToken cancellationToken);
    Task<bool> HasShowtimeAtAsync(Guid auditoriumId, DateTimeOffset startTime, CancellationToken cancellationToken);
    Task AddAsync(Showtime showtime, CancellationToken cancellationToken);
}
