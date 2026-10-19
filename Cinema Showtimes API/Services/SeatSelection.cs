using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;

namespace CinemaShowtimesApi.Services;

/// <summary>
/// Resolves requested seats and applies the seat availability rules for a showtime.
/// </summary>
internal static class SeatSelection
{
    public static List<Seat> ResolveRequested(Showtime showtime, IReadOnlyList<SeatDto> requestedSeats)
    {
        var seatsByLocation = showtime.Auditorium.Seats.ToDictionary(seat => (seat.Row.ToUpperInvariant(), seat.Number));

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

        return resolvedSeats;
    }

    public static HashSet<Guid> GetOccupiedSeatIds(IEnumerable<ShowtimeSeatClaim> showtimeSeatClaims)
    {
        return showtimeSeatClaims.Select(claim => claim.SeatId).ToHashSet();
    }

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
}
