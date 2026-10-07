using CinemaShowtimesApi.Contracts;
using CinemaShowtimesApi.Domain;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Services;
using FluentAssertions;

namespace Cinema_Showtimes_API.Tests;

public class SeatSelectionTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void GetOccupiedSeatIds_AtExactExpiry_TreatsHoldAsFree()
    {
        var showtime = CreateHall();
        Hold(showtime, "A", 1, Now);

        SeatSelection.GetOccupiedSeatIds(showtime, Now).Should().BeEmpty();
    }

    [Test]
    public void GetOccupiedSeatIds_OneTickBeforeExpiry_TreatsHoldAsOccupied()
    {
        var showtime = CreateHall();
        Hold(showtime, "A", 1, Now.AddTicks(1));

        SeatSelection.GetOccupiedSeatIds(showtime, Now).Should().ContainSingle();
    }

    [Test]
    public void ResolveRequested_WhenSeatIsSold_ThrowsSeatsAlreadySold()
    {
        var showtime = CreateHall();
        var occupancy = Occupancy(showtime, "A", 2);
        occupancy.Sell();

        var act = () => SeatSelection.ResolveRequested(showtime, [new SeatDto { Row = "A", Number = 2 }], Now);

        act.Should().Throw<ConflictException>().Where(ex => ex.ErrorCode == "seats_already_sold");
    }

    [Test]
    public void ResolveRequested_WhenSeatIsHeld_ThrowsSeatsCurrentlyReserved()
    {
        var showtime = CreateHall();
        Hold(showtime, "A", 2, Now.AddMinutes(5));

        var act = () => SeatSelection.ResolveRequested(showtime, [new SeatDto { Row = "A", Number = 2 }], Now);

        act.Should().Throw<ConflictException>().Where(ex => ex.ErrorCode == "seats_currently_reserved");
    }

    [Test]
    public void FindContiguousBlock_ReturnsFirstBlockInRowOrder()
    {
        var showtime = CreateHall();
        Hold(showtime, "A", 2, Now.AddMinutes(5));
        var occupied = SeatSelection.GetOccupiedSeatIds(showtime, Now);

        var block = SeatSelection.FindContiguousBlock(showtime.Auditorium.Seats, occupied, 2);

        block.Should().NotBeNull();
        block!.Select(seat => $"{seat.Row}{seat.Number}").Should().Equal("A3", "A4");
    }

    [Test]
    public void FindContiguousBlock_WhenNoBlockFits_ReturnsNull()
    {
        var showtime = CreateHall();
        Hold(showtime, "A", 2, Now.AddMinutes(5));
        Hold(showtime, "B", 2, Now.AddMinutes(5));
        var occupied = SeatSelection.GetOccupiedSeatIds(showtime, Now);

        SeatSelection.FindContiguousBlock(showtime.Auditorium.Seats, occupied, 3).Should().BeNull();
    }

    private static Showtime CreateHall()
    {
        var auditorium = new Auditorium { Id = Guid.NewGuid(), Name = "Hall 1" };
        var showtime = new Showtime { Id = Guid.NewGuid(), Auditorium = auditorium };
        foreach (var row in new[] { "A", "B" })
        {
            for (var number = 1; number <= 4; number++)
            {
                var seat = new Seat { Id = Guid.NewGuid(), Row = row, Number = number, AuditoriumId = auditorium.Id };
                auditorium.Seats.Add(seat);
                showtime.Occupancies.Add(new ShowtimeSeat { ShowtimeId = showtime.Id, SeatId = seat.Id, Seat = seat, Status = ShowtimeSeatStatus.Available });
            }
        }

        return showtime;
    }

    private static ShowtimeSeat Occupancy(Showtime showtime, string row, int number) =>
        showtime.Occupancies.Single(x => x.Seat.Row == row && x.Seat.Number == number);

    private static void Hold(Showtime showtime, string row, int number, DateTime until) =>
        Occupancy(showtime, row, number).Hold(Guid.NewGuid(), until);
}
