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
    private Mock<IReservationRepository> _repository;
    private ReservationService _service;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IReservationRepository>();
        _service = new ReservationService(_repository.Object, TimeProvider.System);
    }

    [Test]
    public void ReserveAsync_WhenSeatsContainDuplicate_ThrowsBusinessRuleException()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        var seats = new[]
        {
            new SeatDto { Row = "A", Number = 1 },
            new SeatDto { Row = "a", Number = 1 }
        };

        // Act
        Func<Task> act = async() => await _service.ReserveAsync(showtimeId, seats, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<BusinessRuleException>();

        _repository.Verify(x => x.GetShowtimeForBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
