using CinemaShowtimesApi.Domain;

namespace CinemaShowtimesApi.Data;

public static class SeedData
{
    public static readonly Guid HallOneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    public static readonly Guid HallTwoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

    public static void Apply(CinemaDbContext db)
    {
        if (!db.Auditoriums.Any())
        {
            db.Auditoriums.AddRange(
                CreateAuditorium(HallOneId, "Hall 1", rows: "ABCDE", seatsPerRow: 10),
                CreateAuditorium(HallTwoId, "Hall 2", rows: "ABCDEF", seatsPerRow: 12));
        }

        if (!db.Movies.Any())
        {
            db.Movies.AddRange(
                new Movie
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                    Title = "The Matrix",
                    Category = "Sci-Fi",
                    Year = 1999,
                    DurationMinutes = 136
                },
                new Movie
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
                    Title = "Amelie",
                    Category = "Romance",
                    Year = 2001,
                    DurationMinutes = 122
                },
                new Movie
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"),
                    Title = "Spirited Away",
                    Category = "Animation",
                    Year = 2001,
                    DurationMinutes = 125
                },
                new Movie
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4"),
                    Title = "Dune: Part Two",
                    Category = "Sci-Fi",
                    Year = 2024,
                    DurationMinutes = 166
                },
                new Movie
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"),
                    Title = "Parasite",
                    Category = "Thriller",
                    Year = 2019,
                    DurationMinutes = 132
                });
        }

        db.SaveChanges();
    }

    private static Auditorium CreateAuditorium(Guid id, string name, string rows, int seatsPerRow)
    {
        var auditorium = new Auditorium { Id = id, Name = name };

        foreach (var row in rows)
        {
            for (var number = 1; number <= seatsPerRow; number++)
            {
                auditorium.Seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    AuditoriumId = id,
                    Row = row.ToString(),
                    Number = number
                });
            }
        }

        return auditorium;
    }
}
