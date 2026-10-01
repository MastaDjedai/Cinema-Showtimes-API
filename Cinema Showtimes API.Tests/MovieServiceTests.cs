using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services;
using FluentAssertions;
using Moq;

namespace Cinema_Showtimes_API.Tests;

public class MovieServiceTests
{
    private Mock<IMovieRepository> _repository;
    private MovieService _service;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IMovieRepository>();
        _service = new MovieService(_repository.Object);
    }

    [Test]
    public async Task CreateAsync_TrimsTextAndPersistsMovie()
    {
        // Arrange
        Movie? savedMovie = null;

        _repository.Setup(x => x.AddAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => savedMovie = movie)
            .Returns(Task.CompletedTask);

        var request = new CreateMovieRequest
        {
            Title = "  Dune  ",
            Category = "  Sci-Fi ",
            Year = 2021,
            DurationMinutes = 155
        };

        // Act
        var response = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        response.Title.Should().Be("Dune");
        response.Category.Should().Be("Sci-Fi");
        savedMovie.Should().NotBeNull();
        _repository.Verify(x => x.AddAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
