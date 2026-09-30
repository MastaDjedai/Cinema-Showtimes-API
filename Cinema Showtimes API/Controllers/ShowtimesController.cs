using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaShowtimesApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ShowtimesController(IShowtimeService showtimeService, IReservationService reservationService) : ControllerBase
{
    [HttpGet("auditoriums")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditoriumResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditoriumResponse>>> ListAuditoriums(CancellationToken cancellationToken)
    {
        return Ok(await showtimeService.ListAuditoriumsAsync(cancellationToken));
    }

    [HttpPost("showtimes")]
    [ProducesResponseType(typeof(ShowtimeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShowtimeResponse>> Create([FromBody] CreateShowtimeRequest request, CancellationToken cancellationToken)
    {
        var showtime = await showtimeService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, showtime);
    }

    [HttpPost("showtimes/{showtimeId:guid}/reservations")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> Reserve(Guid showtimeId, [FromBody] ReserveSeatsRequest request, CancellationToken cancellationToken)
    {
        var reservation = await reservationService.ReserveAsync(showtimeId, request.Seats, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, reservation);
    }

    [HttpPost("showtimes/{showtimeId:guid}/reservations/contiguous")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> ReserveContiguous(Guid showtimeId, [FromBody] ReserveContiguousSeatsRequest request, CancellationToken cancellationToken)
    {
        var reservation = await reservationService.ReserveContiguousAsync(showtimeId, request.SeatCount, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, reservation);
    }
}
