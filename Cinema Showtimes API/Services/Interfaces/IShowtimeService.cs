using CinemaShowtimesApi.Contracts;

namespace CinemaShowtimesApi.Services.Interfaces;

public interface IShowtimeService
{
    Task<IReadOnlyList<AuditoriumResponse>> ListAuditoriumsAsync(CancellationToken cancellationToken);

    Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken cancellationToken);
}
