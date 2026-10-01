using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services;
using FluentAssertions;
using Moq;

namespace Cinema_Showtimes_API.Tests;

public class ShowtimeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private Mock<IShowtimeRepository> _repository;
    private ShowtimeService _service;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IShowtimeRepository>();
        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.GetUtcNow()).Returns(Now);
        _service = new ShowtimeService(_repository.Object, clock.Object);
    }

    [Test]
    public async Task CreateAsync_WhenStartTimeIsInPast_ThrowsBusinessRuleException()
    {
        // Arrange
        var request = new CreateShowtimeRequest
        {
            MovieId = Guid.NewGuid(),
            AuditoriumId = Guid.NewGuid(),
            StartTime = Now.AddHours(-1)
        };

        _repository.Setup(x => x.GetMovieByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Movie
            {
                Id = request.MovieId,
                Title = "Avatar",
                Category = "Sci-Fi",
                Year = 2009
            });

        _repository.Setup(x => x.GetAuditoriumByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Auditorium
            {
                Id = request.AuditoriumId,
                Name = "Hall 1"
            });
        // Act
        Func<Task> act = async() => await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();

        _repository.Verify(x => x.GetMovieByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(x => x.GetAuditoriumByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
