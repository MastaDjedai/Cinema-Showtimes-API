using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories;
using CinemaShowtimesApi.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cinema_Showtimes_API.Tests;

public class BookingConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ReserveAsync_ConcurrentRequestsForTheSameSeat_OnlyOneSucceeds()
    {
        await using var harness = await Harness.CreateAsync();
        var tasks = Enumerable.Range(0, 8).Select(_ => ReserveAsync(harness, throwOnConflict: false)).ToArray();
        var results = await Task.WhenAll(tasks);

        results.Count(response => response is not null).Should().Be(1);
        await using var db = harness.CreateContext();
        (await db.Reservations.CountAsync(x => x.Status == ReservationStatus.Pending)).Should().Be(1);
        (await db.ShowtimeSeats.CountAsync(x => x.Status == ShowtimeSeatStatus.Held)).Should().Be(1);
    }

    [Test]
    public async Task ConfirmAsync_ConcurrentConfirms_OnlyOneSucceeds()
    {
        await using var harness = await Harness.CreateAsync();
        var reserved = await ReserveAsync(harness, throwOnConflict: true);

        var tasks = Enumerable.Range(0, 4).Select(_ => ConfirmAsync(harness, reserved!.ReservationReference)).ToArray();
        var results = await Task.WhenAll(tasks);

        results.Count(succeeded => succeeded).Should().Be(1);
        await using var db = harness.CreateContext();
        (await db.Reservations.CountAsync(x => x.Status == ReservationStatus.Confirmed)).Should().Be(1);
        (await db.ShowtimeSeats.CountAsync(x => x.Status == ShowtimeSeatStatus.Sold)).Should().Be(1);
    }

    private static async Task<ReservationResponse?> ReserveAsync(Harness harness, bool throwOnConflict)
    {
        await using var db = harness.CreateContext();
        var service = CreateService(db);
        try
        {
            return await service.ReserveAsync(harness.ShowtimeId, [harness.Seat], CancellationToken.None);
        }
        catch (ConflictException) when (!throwOnConflict)
        {
            return null;
        }
    }

    private static async Task<bool> ConfirmAsync(Harness harness, Guid reservationId)
    {
        await using var db = harness.CreateContext();
        var service = CreateService(db);
        try
        {
            await service.ConfirmAsync(reservationId, CancellationToken.None);
            return true;
        }
        catch (ConflictException)
        {
            return false;
        }
    }

    private static ReservationService CreateService(CinemaDbContext db)
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.GetUtcNow()).Returns(Now);
        return new ReservationService(new ReservationRepository(db), clock.Object);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _keepAlive;
        private readonly string _connectionString;

        public Guid ShowtimeId { get; private init; }
        public SeatDto Seat { get; private init; } = null!;

        private Harness(SqliteConnection keepAlive, string connectionString, Guid showtimeId, SeatDto seat)
        {
            _keepAlive = keepAlive;
            _connectionString = connectionString;
            ShowtimeId = showtimeId;
            Seat = seat;
        }

        public static async Task<Harness> CreateAsync()
        {
            var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();
            await using var busy = keepAlive.CreateCommand();
            busy.CommandText = "PRAGMA busy_timeout=5000;";
            await busy.ExecuteNonQueryAsync();

            await using var db = CreateContext(connectionString);
            await db.Database.EnsureCreatedAsync();

            var movie = new Movie { Id = Guid.NewGuid(), Title = "Dune", Category = "Sci-Fi", Year = 2021, DurationMinutes = 155 };
            var auditorium = new Auditorium { Id = Guid.NewGuid(), Name = "Hall 1" };
            var seat = new Seat { Id = Guid.NewGuid(), AuditoriumId = auditorium.Id, Row = "A", Number = 1 };
            auditorium.Seats.Add(seat);
            var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = movie.Id, AuditoriumId = auditorium.Id, StartTime = Now.AddHours(3), EndTime = Now.AddHours(6) };
            showtime.Occupancies.Add(new ShowtimeSeat { ShowtimeId = showtime.Id, SeatId = seat.Id, Status = ShowtimeSeatStatus.Available });
            db.Movies.Add(movie);
            db.Auditoriums.Add(auditorium);
            db.Showtimes.Add(showtime);
            await db.SaveChangesAsync();

            return new Harness(keepAlive, connectionString, showtime.Id, new SeatDto { Row = "A", Number = 1 });
        }

        public CinemaDbContext CreateContext() => CreateContext(_connectionString);

        private static CinemaDbContext CreateContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<CinemaDbContext>().UseSqlite(connectionString).Options;
            var db = new CinemaDbContext(options);
            db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
            return db;
        }

        public async ValueTask DisposeAsync() => await _keepAlive.DisposeAsync();
    }
}
