using Cloudweather.Temperature.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TemperatureDbContext>(
    opts =>
    {
        opts.EnableSensitiveDataLogging();
        opts.EnableDetailedErrors();
        opts.UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
    },
    ServiceLifetime.Transient
);

var app = builder.Build();

//Listen for GET /observation and a zip code
app.MapGet("/observation/{zip}", async (string zip, [FromQuery] int? days, TemperatureDbContext db) =>
{
    if (days.HasValue && days.Value >= 1 && days.Value <= 30)
    {
        var startDate = DateTime.UtcNow - TimeSpan.FromDays(days.Value);
        var results = await db.Temperature
            .Where(precip => precip.ZipCode == zip)
            .ToListAsync();
        return Results.Ok(results);
    }
    else
    {
        return Results.BadRequest("Please provide a 'days' query parameter between 1 and 30");
    }
});

app.MapPost("/observation", async (Temperature temperature, TemperatureDbContext db) =>
{
    temperature.CreatedOn = temperature.CreatedOn.ToUniversalTime();
    await db.AddAsync(temperature);
    await db.SaveChangesAsync();
});

app.Run();
