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
        ValidateRequestedSeats(requestedSeats);

        var showtime = await LoadShowtimeAsync(showtimeId, cancellationToken);
        await reservationRepository.ExpirePendingAsync(showtimeId, UtcNow(), cancellationToken);

        var seats = ResolveSeats(showtime.Auditorium, requestedSeats);
        EnsureSeatsAvailable(showtime, seats.Select(x => x.Id).ToHashSet());

        return await CreateReservationAsync(showtime, seats, cancellationToken);
    }

    public async Task<ReservationResponse> ReserveContiguousAsync(Guid showtimeId, int seatCount, CancellationToken cancellationToken)
    {
        var showtime = await LoadShowtimeAsync(showtimeId, cancellationToken);
        await reservationRepository.ExpirePendingAsync(showtimeId, UtcNow(), cancellationToken);

        var occupied = GetOccupiedSeatIds(showtime);
        var block = FindContiguousBlock(showtime.Auditorium.Seats, occupied, seatCount)
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

    private static void ValidateRequestedSeats(IReadOnlyList<SeatDto> requestedSeats)
    {
        var duplicates = requestedSeats
            .GroupBy(x => (x.Row.ToUpperInvariant(), x.Number))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Item1}{g.Key.Number}")
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new BusinessRuleException($"Duplicate seats in the request: {string.Join(", ", duplicates)}.", "duplicate_seats");
        }
    }

    private static List<Seat> ResolveSeats(Auditorium auditorium, IReadOnlyList<SeatDto> requestedSeats)
    {
        var lookup = auditorium.Seats.ToDictionary(x => (x.Row, x.Number));
        var resolved = new List<Seat>(requestedSeats.Count);
        var missing = new List<string>();

        foreach (var requested in requestedSeats)
        {
            var row = requested.Row.ToUpperInvariant();
            if (!lookup.TryGetValue((row, requested.Number), out var seat))
            {
                missing.Add($"{row}{requested.Number}");
                continue;
            }

            resolved.Add(seat);
        }

        if (missing.Count > 0)
        {
            throw new BusinessRuleException($"Seats do not exist in {auditorium.Name}: {string.Join(", ", missing)}.", "unknown_seats");
        }

        return resolved;
    }

    private HashSet<Guid> GetOccupiedSeatIds(Showtime showtime)
    {
        var now = UtcNow();

        return showtime.Reservations
            .Where(r => r.Status == ReservationStatus.Confirmed || r.IsPending(now))
            .SelectMany(r => r.Seats)
            .Select(s => s.SeatId)
            .ToHashSet();
    }

    private void EnsureSeatsAvailable(Showtime showtime, HashSet<Guid> requestedSeatIds)
    {
        var now = UtcNow();

        var sold = showtime.Reservations
            .Where(r => r.Status == ReservationStatus.Confirmed)
            .SelectMany(r => r.Seats)
            .Where(s => requestedSeatIds.Contains(s.SeatId))
            .Select(s => s.Seat)
            .ToList();

        if (sold.Count > 0)
        {
            throw new ConflictException($"Seats already sold: {FormatSeats(sold)}.", "seats_already_sold");
        }

        var reserved = showtime.Reservations
            .Where(r => r.IsPending(now))
            .SelectMany(r => r.Seats)
            .Where(s => requestedSeatIds.Contains(s.SeatId))
            .Select(s => s.Seat)
            .ToList();

        if (reserved.Count > 0)
        {
            throw new ConflictException($"Seats currently reserved: {FormatSeats(reserved)}.", "seats_currently_reserved");
        }
    }

    private static List<Seat>? FindContiguousBlock(IEnumerable<Seat> seats, HashSet<Guid> occupied, int seatCount)
    {
        var byRow = seats.GroupBy(s => s.Row).OrderBy(g => g.Key);

        foreach (var row in byRow)
        {
            var available = row.Where(s => !occupied.Contains(s.Id)).OrderBy(s => s.Number).ToList();

            for (var i = 0; i <= available.Count - seatCount; i++)
            {
                var candidate = available.Skip(i).Take(seatCount).ToList();
                var isContiguous = candidate.Zip(candidate.Skip(1), (left, right) => right.Number == left.Number + 1).All(x => x);

                if (isContiguous)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string FormatSeats(IEnumerable<Seat> seats) =>
        string.Join(", ", seats.Where(s => s is not null).Select(s => $"{s.Row}{s.Number}").Distinct().OrderBy(x => x));

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
