using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Services;

public sealed class ShowtimeService(CinemaDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AuditoriumResponse>> ListAuditoriumsAsync(CancellationToken cancellationToken)
    {
        var auditoriums = await db.Auditoriums
            .AsNoTracking()
            .Include(x => x.Seats)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return auditoriums.Select(a => new AuditoriumResponse
        {
            Id = a.Id,
            Name = a.Name,
            Seats = a.Seats
                .OrderBy(s => s.Row)
                .ThenBy(s => s.Number)
                .Select(s => new SeatDto { Row = s.Row, Number = s.Number })
                .ToList()
        }).ToList();
    }

    public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken cancellationToken)
    {
        if (request.MovieId == Guid.Empty || request.AuditoriumId == Guid.Empty)
        {
            throw new BusinessRuleException("MovieId and AuditoriumId are required.", "missing_ids");
        }

        var movie = await db.Movies.FirstOrDefaultAsync(x => x.Id == request.MovieId, cancellationToken)
            ?? throw new NotFoundException($"Movie '{request.MovieId}' was not found.", "movie_not_found");

        var auditorium = await db.Auditoriums.FirstOrDefaultAsync(x => x.Id == request.AuditoriumId, cancellationToken)
            ?? throw new NotFoundException($"Auditorium '{request.AuditoriumId}' was not found.", "auditorium_not_found");

        if (request.StartTime <= timeProvider.GetUtcNow())
        {
            throw new BusinessRuleException("Showtime start time must be in the future.", "showtime_in_the_past");
        }

        var overlapping = await db.Showtimes.AnyAsync(
            x => x.AuditoriumId == request.AuditoriumId && x.StartTime == request.StartTime,
            cancellationToken);

        if (overlapping)
        {
            throw new ConflictException(
                "This auditorium already has a showtime at the requested start time.",
                "showtime_overlap");
        }

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = movie.Id,
            AuditoriumId = auditorium.Id,
            StartTime = request.StartTime.ToUniversalTime()
        };

        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync(cancellationToken);

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
