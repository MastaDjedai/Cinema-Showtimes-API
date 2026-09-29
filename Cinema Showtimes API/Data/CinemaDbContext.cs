using CinemaShowtimesApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Data;

public class CinemaDbContext(DbContextOptions<CinemaDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Auditorium> Auditoriums => Set<Auditorium>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationSeat> ReservationSeats => Set<ReservationSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Auditorium>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Row).HasMaxLength(2).IsRequired();
            entity.HasIndex(x => new { x.AuditoriumId, x.Row, x.Number }).IsUnique();
            entity.HasOne(x => x.Auditorium)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.AuditoriumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Movie)
                .WithMany(x => x.Showtimes)
                .HasForeignKey(x => x.MovieId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Auditorium)
                .WithMany(x => x.Showtimes)
                .HasForeignKey(x => x.AuditoriumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("TEXT");
            entity.Property(x => x.ExpiresAt).HasColumnType("TEXT");
            entity.HasOne(x => x.Showtime)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationSeat>(entity =>
        {
            entity.HasKey(x => new { x.ReservationId, x.SeatId });
            entity.HasOne(x => x.Reservation)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Seat)
                .WithMany(x => x.ReservationSeats)
                .HasForeignKey(x => x.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
