using MySqlConnector;
using Server.Api.Auth;
using Server.Api.Data;
using Server.Api.Features.AdminDashboard;
using Server.Api.Features.Asignaciones;
using Server.Api.Features.Comisiones;
using Server.Api.Features.Dispositivos;
using Server.Api.Features.Equipos;
using Server.Api.Features.Estadios;
using Server.Api.Features.Eventos;
using Server.Api.Features.Reportes;
using Server.Api.Features.Transferencias;
using Server.Api.Features.Usuarios;
using Server.Api.Features.Validaciones;
using Server.Api.Features.Ventas;
using Server.Api.Infrastructure;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No connection string configured.");

// IgnoreCommandTransaction: los comandos creados con conn.CreateCommand() dentro
// de una transacción (ver Db.TransactionAsync) participan de la transacción activa
// sin tener que asignarles cmd.Transaction a mano.
var dataSourceConnString = new MySqlConnectionStringBuilder(connString)
{
    IgnoreCommandTransaction = true,
}.ConnectionString;

builder.Services.AddSingleton(new MySqlDataSource(dataSourceConnString));
builder.Services.AddSingleton<Db>();

// CORS para el front de React. En desarrollo Vite puede arrancar en cualquier
// puerto de localhost (5173, 5174, …), así que aceptamos cualquiera de localhost
// en vez de atarnos a uno fijo.
const string FrontPolicy = "front";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontPolicy, policy =>
        policy.SetIsOriginAllowed(origin =>
                {
                    var host = new Uri(origin).Host;
                    return host is "localhost" or "127.0.0.1";
                })
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors(FrontPolicy);

app.UseMiddleware<ErrorHandlingMiddleware>();

// ---------- Health ----------
app.MapGet("/health", async (Db db) =>
{
    var pong = await db.ScalarAsync<string>("SELECT 'pong'");
    return Results.Ok(new PingResult(pong ?? "?"));
});

// ---------- Cimientos: autenticación ----------
app.MapAuth();
app.MapAdminDashboard();

// ============================================================
//  Puntos de extensión — cada persona agrega su grupo de endpoints.
//  Crear el archivo en server/Features/<Dominio>/<Dominio>Endpoints.cs
//  con un método  public static void Map<Dominio>(this IEndpointRouteBuilder app)
//  y registrarlo acá.
// ============================================================

app.MapUsuarios();
app.MapEquipos();
app.MapEstadios();
app.MapEventos();
app.MapComisiones();
app.MapVentas();
app.MapAsignaciones();
app.MapDispositivos();
app.MapReportes();
app.MapValidaciones();
app.MapTransferencias();

app.Run();
