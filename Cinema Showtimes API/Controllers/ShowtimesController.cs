using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaShowtimesApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ShowtimesController(ShowtimeService showtimes, ReservationService reservations) : ControllerBase
{
    [HttpGet("auditoriums")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditoriumResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditoriumResponse>>> ListAuditoriums(
        CancellationToken cancellationToken)
    {
        return Ok(await showtimes.ListAuditoriumsAsync(cancellationToken));
    }

    [HttpPost("showtimes")]
    [ProducesResponseType(typeof(ShowtimeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShowtimeResponse>> Create(
        [FromBody] CreateShowtimeRequest request,
        CancellationToken cancellationToken)
    {
        var showtime = await showtimes.CreateAsync(request, cancellationToken);
        return Created($"/api/showtimes/{showtime.Id}", showtime);
    }

    [HttpPost("showtimes/{showtimeId:guid}/reservations")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> Reserve(
        Guid showtimeId,
        [FromBody] ReserveSeatsRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.ReserveAsync(showtimeId, request.Seats, cancellationToken);
        return Created($"/api/reservations/{reservation.ReservationReference}", reservation);
    }

    [HttpPost("showtimes/{showtimeId:guid}/reservations/contiguous")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> ReserveContiguous(
        Guid showtimeId,
        [FromBody] ReserveContiguousSeatsRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await reservations.ReserveContiguousAsync(showtimeId, request.SeatCount, cancellationToken);
        return Created($"/api/reservations/{reservation.ReservationReference}", reservation);
    }
}
