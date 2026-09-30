using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Repositories;
using CinemaShowtimesApi.Repositories.Interfaces;
using CinemaShowtimesApi.Services;
using CinemaShowtimesApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CinemaShowtimesApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCinemaApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<CinemaDbContext>(options => options.UseSqlite(configuration.GetConnectionString("Cinema") ?? "Data Source=cinema.db"));

        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();

        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<IReservationService, ReservationService>();

        return services;
    }
}
