using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services.Interfaces;

namespace CinemaShowtimesApi.Services;

public sealed class ShowtimeService(IShowtimeRepository showtimeRepository, TimeProvider timeProvider) : IShowtimeService
{
    public async Task<IReadOnlyList<AuditoriumResponse>> ListAuditoriumsAsync(CancellationToken cancellationToken)
    {
        var auditoriums = await showtimeRepository.ListAuditoriumsWithSeatsAsync(cancellationToken);

        return auditoriums.Select(a => new AuditoriumResponse
        {
            Id = a.Id,
            Name = a.Name,
            Seats = a.Seats.OrderBy(s => s.Row).ThenBy(s => s.Number).Select(s => new SeatDto { Row = s.Row, Number = s.Number }).ToList()
        }).ToList();
    }

    public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken cancellationToken)
    {
        if (request.MovieId == Guid.Empty || request.AuditoriumId == Guid.Empty)
        {
            throw new BusinessRuleException("MovieId and AuditoriumId are required.", "missing_ids");
        }

        var movie = await showtimeRepository.GetMovieByIdAsync(request.MovieId, cancellationToken)
            ?? throw new NotFoundException($"Movie '{request.MovieId}' was not found.", "movie_not_found");

        var auditorium = await showtimeRepository.GetAuditoriumByIdAsync(request.AuditoriumId, cancellationToken)
            ?? throw new NotFoundException($"Auditorium '{request.AuditoriumId}' was not found.", "auditorium_not_found");

        if (request.StartTime <= timeProvider.GetUtcNow())
        {
            throw new BusinessRuleException("Showtime start time must be in the future.", "showtime_in_the_past");
        }

        if (await showtimeRepository.HasShowtimeAtAsync(request.AuditoriumId, request.StartTime.ToUniversalTime(), cancellationToken))
        {
            throw new ConflictException("This auditorium already has a showtime at the requested start time.", "showtime_overlap");
        }

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = movie.Id,
            AuditoriumId = auditorium.Id,
            StartTime = request.StartTime.ToUniversalTime()
        };

        await showtimeRepository.AddAsync(showtime, cancellationToken);

        return new ShowtimeResponse
        {
            Id = showtime.Id,
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            AuditoriumId = auditorium.Id,
            AuditoriumName = auditorium.Name,
            StartTime = showtime.StartTime
        };
    }
}
