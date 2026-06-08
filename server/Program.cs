using Npgsql;
using Shared;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", async (IConfiguration config) =>
{
    var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("No connection string configured.");

    try
    {
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT fn_ping()", conn);
        var result = (string)(await cmd.ExecuteScalarAsync())!;
        return Results.Ok(new PingResult(result));
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database unavailable");
    }
});

app.Run();
