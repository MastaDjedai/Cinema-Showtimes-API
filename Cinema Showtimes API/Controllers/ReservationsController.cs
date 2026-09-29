using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaShowtimesApi.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(ReservationService reservations) : ControllerBase
{
    [HttpPost("{reservationReference:guid}/confirm")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Confirm(
        Guid reservationReference,
        CancellationToken cancellationToken)
    {
        return Ok(await reservations.ConfirmAsync(reservationReference, cancellationToken));
    }
}
