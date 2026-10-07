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

    private Mock<IShowtimeRepository> _repository = null!;
    private ShowtimeService _service = null!;

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
        var request = new CreateShowtimeRequest { MovieId = Guid.NewGuid(), AuditoriumId = Guid.NewGuid(), StartTime = Now.AddHours(-1) };
        SetupMovieAndHall(request);

        var act = () => _service.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().Where(ex => ex.ErrorCode == "showtime_in_the_past");
    }

    [Test]
    public async Task CreateAsync_WhenAnotherShowOverlapsDuration_ThrowsConflict()
    {
        var request = new CreateShowtimeRequest { MovieId = Guid.NewGuid(), AuditoriumId = Guid.NewGuid(), StartTime = Now.AddHours(1) };
        SetupMovieAndHall(request, durationMinutes: 136);
        _repository.Setup(x => x.HasOverlappingShowtimeAsync(request.AuditoriumId!.Value, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().Where(ex => ex.ErrorCode == "showtime_overlap");
        _repository.Verify(x => x.HasOverlappingShowtimeAsync(request.AuditoriumId!.Value, request.StartTime!.Value, request.StartTime.Value.AddMinutes(136), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupMovieAndHall(CreateShowtimeRequest request, int durationMinutes = 120)
    {
        _repository.Setup(x => x.GetMovieByIdAsync(request.MovieId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Movie { Id = request.MovieId!.Value, Title = "The Matrix", Category = "Sci-Fi", Year = 1999, DurationMinutes = durationMinutes });
        _repository.Setup(x => x.GetAuditoriumByIdAsync(request.AuditoriumId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Auditorium { Id = request.AuditoriumId!.Value, Name = "Hall 1" });
    }
}
