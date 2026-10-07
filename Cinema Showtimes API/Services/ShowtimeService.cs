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
        var movieId = request.MovieId!.Value;
        var auditoriumId = request.AuditoriumId!.Value;
        var startTime = request.StartTime!.Value.ToUniversalTime();

        var movie = await showtimeRepository.GetMovieByIdAsync(movieId, cancellationToken)
            ?? throw new NotFoundException($"Movie '{movieId}' was not found.", "movie_not_found");

        var auditorium = await showtimeRepository.GetAuditoriumByIdAsync(auditoriumId, cancellationToken)
            ?? throw new NotFoundException($"Auditorium '{auditoriumId}' was not found.", "auditorium_not_found");

        if (startTime <= timeProvider.GetUtcNow())
        {
            throw new BusinessRuleException("Showtime start time must be in the future.", "showtime_in_the_past");
        }

        var endTime = startTime.AddMinutes(movie.DurationMinutes);
        if (await showtimeRepository.HasOverlappingShowtimeAsync(auditoriumId, startTime, endTime, cancellationToken))
        {
            throw new ConflictException("This auditorium already has a showtime that overlaps the requested time.", "showtime_overlap");
        }

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = movie.Id,
            AuditoriumId = auditorium.Id,
            StartTime = startTime,
            EndTime = endTime
        };

        await showtimeRepository.AddAsync(showtime, cancellationToken);

        return new ShowtimeResponse
        {
            Id = showtime.Id,
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            AuditoriumId = auditorium.Id,
            AuditoriumName = auditorium.Name,
            StartTime = showtime.StartTime,
            EndTime = showtime.EndTime
        };
    }
}
