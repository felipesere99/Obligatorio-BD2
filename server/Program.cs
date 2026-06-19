using Npgsql;
using Server.Api.Auth;
using Server.Api.Data;
using Server.Api.Features.Comisiones;
using Server.Api.Features.Equipos;
using Server.Api.Features.Estadios;
using Server.Api.Features.Eventos;
using Server.Api.Features.Usuarios;
using Server.Api.Infrastructure;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No connection string configured.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connString));
builder.Services.AddSingleton<Db>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// ---------- Health ----------
app.MapGet("/health", async (Db db) =>
{
    var pong = await db.ScalarAsync<string>("SELECT fn_ping()");
    return Results.Ok(new PingResult(pong ?? "?"));
});

// ---------- Cimientos: autenticación ----------
app.MapAuth();

// ============================================================
//  Puntos de extensión — cada persona agrega su grupo de endpoints.
//  Crear el archivo en server/Features/<Dominio>/<Dominio>Endpoints.cs
//  con un método  public static void Map<Dominio>(this IEndpointRouteBuilder app)
//  y registrarlo acá.
// ============================================================

// ---------- Persona A ----------
app.MapUsuarios();
app.MapEquipos();
app.MapEstadios();
app.MapEventos();
app.MapComisiones();

app.Run();
