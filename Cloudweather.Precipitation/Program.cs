using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

//Listen for GET /observation and a zip code
app.MapGet("/observation/{zip}", (string zip, [FromQuery] int? days ) =>
{
    return Results.Ok(zip);
});

app.Run();
