using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;

namespace CinemaShowtimesApi.Services;

/// <summary>
/// Resolves requested seats and applies the seat availability rules for a showtime.
/// </summary>
internal static class SeatSelection
{
    public static List<Seat> ResolveRequested(Showtime showtime, IReadOnlyList<SeatDto> requestedSeats, DateTime utcNow)
    {
        var seatsByLocation = showtime.Auditorium.Seats
            .ToDictionary(seat => (seat.Row.ToUpperInvariant(), seat.Number));
        var resolvedSeats = new List<Seat>(requestedSeats.Count);
        var missingSeats = new List<string>();

        foreach (var requested in requestedSeats)
        {
            var row = requested.Row.ToUpperInvariant();
            if (!seatsByLocation.TryGetValue((row, requested.Number), out var seat))
            {
                missingSeats.Add($"{row}{requested.Number}");
                continue;
            }

            resolvedSeats.Add(seat);
        }

        if (missingSeats.Count > 0)
        {
            throw new BusinessRuleException(
                $"Seats do not exist in {showtime.Auditorium.Name}: {string.Join(", ", missingSeats)}.",
                "unknown_seats");
        }

        EnsureAvailable(showtime, resolvedSeats.Select(seat => seat.Id).ToHashSet(), utcNow);
        return resolvedSeats;
    }

    public static HashSet<Guid> GetOccupiedSeatIds(Showtime showtime, DateTime utcNow) => showtime.Reservations
        .Where(reservation => reservation.Status == ReservationStatus.Confirmed || reservation.IsPending(utcNow))
        .SelectMany(reservation => reservation.Seats)
        .Select(reservationSeat => reservationSeat.SeatId)
        .ToHashSet();

    public static List<Seat>? FindContiguousBlock(IEnumerable<Seat> seats, HashSet<Guid> occupied, int seatCount)
    {
        foreach (var row in seats.GroupBy(seat => seat.Row).OrderBy(group => group.Key))
        {
            var available = row
                .Where(seat => !occupied.Contains(seat.Id))
                .OrderBy(seat => seat.Number)
                .ToList();

            var block = new List<Seat>(seatCount);
            foreach (var seat in available)
            {
                if (block.Count > 0 && seat.Number != block[^1].Number + 1)
                {
                    block.Clear();
                }

                block.Add(seat);
                if (block.Count == seatCount)
                {
                    return block;
                }
            }
        }

        return null;
    }

    public static void ValidateNoDuplicates(IReadOnlyList<SeatDto> requestedSeats)
    {
        var duplicates = requestedSeats
            .GroupBy(seat => (seat.Row.ToUpperInvariant(), seat.Number))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.Item1}{group.Key.Number}")
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new BusinessRuleException(
                $"Duplicate seats in the request: {string.Join(", ", duplicates)}.",
                "duplicate_seats");
        }
    }

    private static void EnsureAvailable(Showtime showtime, HashSet<Guid> requestedSeatIds, DateTime utcNow)
    {
        var matchingReservations = showtime.Reservations
            .Where(reservation => reservation.Status == ReservationStatus.Confirmed || reservation.IsPending(utcNow))
            .SelectMany(reservation => reservation.Seats
                .Where(seat => requestedSeatIds.Contains(seat.SeatId))
                .Select(seat => (reservation.Status, seat.Seat)))
            .ToList();

        var sold = matchingReservations
            .Where(match => match.Status == ReservationStatus.Confirmed)
            .Select(match => match.Seat)
            .ToList();
        if (sold.Count > 0)
        {
            throw new ConflictException($"Seats already sold: {FormatSeats(sold)}.", "seats_already_sold");
        }

        var reserved = matchingReservations
            .Where(match => match.Status == ReservationStatus.Pending)
            .Select(match => match.Seat)
            .ToList();
        if (reserved.Count > 0)
        {
            throw new ConflictException($"Seats currently reserved: {FormatSeats(reserved)}.", "seats_currently_reserved");
        }
    }

    private static string FormatSeats(IEnumerable<Seat> seats) =>
        string.Join(", ", seats.DistinctBy(seat => seat.Id).OrderBy(seat => seat.Row).ThenBy(seat => seat.Number)
            .Select(seat => $"{seat.Row}{seat.Number}"));
}
