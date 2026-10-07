using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services;
using FluentAssertions;
using Moq;

namespace Cinema_Showtimes_API.Tests;

public class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private Mock<IReservationRepository> _repository = null!;
    private ReservationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IReservationRepository>();
        _repository
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<ReservationResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<ReservationResponse>>, CancellationToken>((action, _) => action());

        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.GetUtcNow()).Returns(Now);
        _service = new ReservationService(_repository.Object, clock.Object);
    }

    [Test]
    public async Task ReserveAsync_WhenSeatsContainDuplicate_ThrowsBusinessRuleException()
    {
        var seats = new[] { new SeatDto { Row = "A", Number = 1 }, new SeatDto { Row = "a", Number = 1 } };

        var act = () => _service.ReserveAsync(Guid.NewGuid(), seats, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().Where(ex => ex.ErrorCode == "duplicate_seats");
        _repository.Verify(x => x.GetShowtimeForBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ConfirmAsync_WhenReservationIsPendingAndNotExpired_MarksItConfirmed()
    {
        var reservation = CreatePendingReservation(Now.UtcDateTime.AddMinutes(5));
        _repository.Setup(x => x.GetByIdForConfirmAsync(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        var response = await _service.ConfirmAsync(reservation.Id, CancellationToken.None);

        response.Status.Should().Be(nameof(ReservationStatus.Confirmed));
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.Showtime.Occupancies.Single().Status.Should().Be(ShowtimeSeatStatus.Sold);
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConfirmAsync_WhenExpiresAtIsExactlyNow_ThrowsReservationExpired()
    {
        var reservation = CreatePendingReservation(Now.UtcDateTime);
        _repository.Setup(x => x.GetByIdForConfirmAsync(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        var act = () => _service.ConfirmAsync(reservation.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().Where(ex => ex.ErrorCode == "reservation_expired");
        reservation.Status.Should().Be(ReservationStatus.Expired);
    }

    [Test]
    public async Task ConfirmAsync_WhenExpiresAtIsOneTickBeforeNow_Succeeds()
    {
        var reservation = CreatePendingReservation(Now.UtcDateTime.AddTicks(1));
        _repository.Setup(x => x.GetByIdForConfirmAsync(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        var response = await _service.ConfirmAsync(reservation.Id, CancellationToken.None);

        response.Status.Should().Be(nameof(ReservationStatus.Confirmed));
    }

    [Test]
    public async Task ConfirmAsync_WhenAlreadyConfirmed_ThrowsConflict()
    {
        var reservation = CreatePendingReservation(Now.UtcDateTime.AddMinutes(5));
        reservation.Status = ReservationStatus.Confirmed;
        _repository.Setup(x => x.GetByIdForConfirmAsync(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        var act = () => _service.ConfirmAsync(reservation.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().Where(ex => ex.ErrorCode == "already_confirmed");
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReserveAsync_WhenShowtimeHasStarted_ThrowsBusinessRuleException()
    {
        var showtime = CreateShowtime(Now.AddMinutes(-1));
        _repository.Setup(x => x.GetShowtimeForBookingAsync(showtime.Id, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);

        var act = () => _service.ReserveAsync(showtime.Id, [new SeatDto { Row = "A", Number = 1 }], CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().Where(ex => ex.ErrorCode == "showtime_already_started");
    }

    private static Reservation CreatePendingReservation(DateTime expiresAt)
    {
        var showtime = CreateShowtime(Now.AddHours(2));
        var seat = showtime.Auditorium.Seats.Single();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ShowtimeId = showtime.Id,
            Showtime = showtime,
            Status = ReservationStatus.Pending,
            CreatedAt = expiresAt.AddMinutes(-Reservation.DefaultTtlMinutes),
            ExpiresAt = expiresAt,
            Seats = { new ReservationSeat { SeatId = seat.Id, Seat = seat } }
        };
        showtime.Occupancies.Single().Hold(reservation.Id, expiresAt);
        return reservation;
    }

    private static Showtime CreateShowtime(DateTimeOffset startTime)
    {
        var seat = new Seat { Id = Guid.NewGuid(), Row = "A", Number = 1 };
        var auditorium = new Auditorium { Id = Guid.NewGuid(), Name = "Hall 1", Seats = { seat } };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Dune", Category = "Sci-Fi", Year = 2021, DurationMinutes = 155 };
        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            Movie = movie,
            Auditorium = auditorium,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(movie.DurationMinutes)
        };
        showtime.Occupancies.Add(new ShowtimeSeat { ShowtimeId = showtime.Id, SeatId = seat.Id, Seat = seat, Status = ShowtimeSeatStatus.Available });
        return showtime;
    }
}
