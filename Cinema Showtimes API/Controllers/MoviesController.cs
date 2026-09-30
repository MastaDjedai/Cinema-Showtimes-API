using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaShowtimesApi.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(IMovieService movies) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MovieResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await movies.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovieResponse>> Create([FromBody] CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movie = await movies.CreateAsync(request, cancellationToken);
        return Created($"/api/movies/{movie.Id}", movie);
    }
}
