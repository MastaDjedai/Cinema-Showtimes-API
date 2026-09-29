using CinemaShowtimesApi.Data;
using CinemaShowtimesApi.Errors;
using CinemaShowtimesApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<ShowtimeService>();
builder.Services.AddScoped<ReservationService>();

var connectionString = builder.Configuration.GetConnectionString("Cinema")
    ?? "Data Source=cinema.db";

builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    db.Database.EnsureCreated();
    SeedData.Apply(db);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
