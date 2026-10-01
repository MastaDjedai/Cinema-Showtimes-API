using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services.Interfaces;

namespace CinemaShowtimesApi.Services;

public sealed class ReservationService(IReservationRepository reservationRepository, TimeProvider timeProvider) : IReservationService
{
    public async Task<ReservationResponse> ReserveAsync(Guid showtimeId, IReadOnlyList<SeatDto> requestedSeats, CancellationToken cancellationToken)
    {
        SeatSelection.ValidateNoDuplicates(requestedSeats);
        var showtime = await PrepareShowtimeForBookingAsync(showtimeId, cancellationToken);

        var seats = SeatSelection.ResolveRequested(showtime, requestedSeats, UtcNow());

        return await CreateReservationAsync(showtime, seats, cancellationToken);
    }

    public async Task<ReservationResponse> ReserveContiguousAsync(Guid showtimeId, int seatCount, CancellationToken cancellationToken)
    {
        if (seatCount <= 0)
        {
            throw new BusinessRuleException("Seat count must be greater than zero.", "invalid_seat_count");
        }

        var showtime = await PrepareShowtimeForBookingAsync(showtimeId, cancellationToken);

        var now = UtcNow();
        var occupied = SeatSelection.GetOccupiedSeatIds(showtime, now);
        var block = SeatSelection.FindContiguousBlock(showtime.Auditorium.Seats, occupied, seatCount)
            ?? throw new ConflictException($"No contiguous block of {seatCount} seats is available in {showtime.Auditorium.Name}.", "no_contiguous_block");

        return await CreateReservationAsync(showtime, block, cancellationToken);
    }

    public async Task<ReservationResponse> ConfirmAsync(Guid reservationReference, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var reservation = await reservationRepository.GetByIdForConfirmAsync(reservationReference, cancellationToken)
            ?? throw new NotFoundException($"Reservation '{reservationReference}' was not found.", "reservation_not_found");

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            throw new ConflictException("This reservation has already been confirmed.", "already_confirmed");
        }

        if (reservation.Status == ReservationStatus.Expired || reservation.ExpiresAt <= now)
        {
            if (reservation.Status == ReservationStatus.Pending)
            {
                reservation.Status = ReservationStatus.Expired;
                await reservationRepository.SaveChangesAsync(cancellationToken);
            }

            throw new ConflictException("This reservation has expired and can no longer be confirmed.", "reservation_expired");
        }

        reservation.Status = ReservationStatus.Confirmed;
        await reservationRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(reservation);
    }

    private async Task<Showtime> LoadShowtimeAsync(Guid showtimeId, CancellationToken cancellationToken)
    {
        return await reservationRepository.GetShowtimeForBookingAsync(showtimeId, cancellationToken)
            ?? throw new NotFoundException($"Showtime '{showtimeId}' was not found.", "showtime_not_found");
    }

    private async Task<Showtime> PrepareShowtimeForBookingAsync(Guid showtimeId, CancellationToken cancellationToken)
    {
        var showtime = await LoadShowtimeAsync(showtimeId, cancellationToken);
        await reservationRepository.ExpirePendingAsync(showtimeId, UtcNow(), cancellationToken);
        return showtime;
    }

    private async Task<ReservationResponse> CreateReservationAsync(Showtime showtime, IReadOnlyList<Seat> seats, CancellationToken cancellationToken)
    {
        var now = UtcNow();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ShowtimeId = showtime.Id,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(Reservation.DefaultTtlMinutes)
        };

        foreach (var seat in seats)
        {
            reservation.Seats.Add(new ReservationSeat { SeatId = seat.Id, Seat = seat });
        }

        await reservationRepository.AddAsync(reservation, cancellationToken);
        reservation.Showtime = showtime;
        return ToResponse(reservation);
    }

    private static ReservationResponse ToResponse(Reservation reservation)
    {
        var seats = reservation.Seats
            .Select(x => x.Seat)
            .Where(x => x is not null)
            .OrderBy(x => x.Row)
            .ThenBy(x => x.Number)
            .Select(x => new SeatDto { Row = x.Row, Number = x.Number })
            .ToList();

        return new ReservationResponse
        {
            ReservationReference = reservation.Id,
            SeatCount = seats.Count,
            Auditorium = reservation.Showtime.Auditorium.Name,
            Movie = reservation.Showtime.Movie.Title,
            Status = reservation.Status.ToString(),
            ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(reservation.ExpiresAt, DateTimeKind.Utc)),
            Seats = seats
        };
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
