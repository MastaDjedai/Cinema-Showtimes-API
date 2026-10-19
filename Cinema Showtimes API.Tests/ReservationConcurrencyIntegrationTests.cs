using System.Net;
using System.Net.Http.Json;
using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Showtimes_API.Tests;

public sealed class ReservationConcurrencyIntegrationTests
{
    private const int ConcurrentRequests = 12;

    private string _databasePath = null!;
    private string _connectionString = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cinema-race-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_databasePath};Default Timeout=30;Pooling=False";

        var options = new DbContextOptionsBuilder<CinemaDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new CinemaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Cinema", _connectionString));

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Test]
    public async Task ConcurrentReservationsForSameSeat_CreateOnlyOneReservation()
    {
        var showtimeResponse = await _client.PostAsJsonAsync("/api/showtimes", new CreateShowtimeRequest
        {
            MovieId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            AuditoriumId = SeedData.HallOneId,
            StartTime = DateTimeOffset.UtcNow.AddDays(1)
        });
        showtimeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var showtime = await showtimeResponse.Content.ReadFromJsonAsync<ShowtimeResponse>();
        showtime.Should().NotBeNull();

        using var startGate = new ManualResetEventSlim(false);

        var requests = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                startGate.Wait();
                return await _client.PostAsJsonAsync(
                    $"/api/showtimes/{showtime!.Id}/reservations",
                    new ReserveSeatsRequest { Seats = [new SeatDto { Row = "A", Number = 1 }] });
            }))
            .ToArray();

        startGate.Set();
        var responses = await Task.WhenAll(requests);

        responses.Count(x => x.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict).Should().Be(ConcurrentRequests - 1);

        var conflicts = responses.Where(x => x.StatusCode == HttpStatusCode.Conflict).ToArray();
        foreach (var response in conflicts)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Extensions["errorCode"]!.ToString().Should().Be("seats_unavailable");
        }

        var dbOptions = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        await using var verificationDb = new CinemaDbContext(dbOptions);
        var claims = await verificationDb.ShowtimeSeatClaims
            .Where(x => x.ShowtimeId == showtime!.Id)
            .ToListAsync();
        claims.Should().ContainSingle();

        var reservationsWithSeat = await verificationDb.ReservationSeats
            .Where(x => x.Seat.Row == "A" && x.Seat.Number == 1 && x.Reservation.ShowtimeId == showtime.Id)
            .CountAsync();
        reservationsWithSeat.Should().Be(1);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
