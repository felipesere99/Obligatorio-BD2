using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Estadios;

public static class EstadiosEndpoints
{
    public static void MapEstadios(this IEndpointRouteBuilder app)
    {
        // POST /estadios (admin) -> alta de un estadio.
        app.MapPost("/estadios", async (HttpContext ctx, RegistrarEstadioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var nombre = await db.ScalarAsync<string>(
                "CALL sp_registrar_estadio(@nombre, @direccion)",
                p =>
                {
                    p.AddWithValue("nombre", req.Nombre);
                    p.AddWithValue("direccion", (object?)req.Direccion ?? DBNull.Value);
                });

            return Results.Created($"/estadios/{nombre}", new { nombre });
        });

        // POST /estadios/{nombre}/sectores (admin) -> alta de un sector del estadio.
        app.MapPost("/estadios/{nombre}/sectores", async (string nombre, HttpContext ctx, RegistrarSectorRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var sector = await db.ScalarAsync<string>(
                "CALL sp_registrar_sector(@estadio, @nombre, @capacidad, @costo)",
                p =>
                {
                    p.AddWithValue("estadio", nombre);
                    p.AddWithValue("nombre", req.Nombre);
                    p.AddWithValue("capacidad", req.Capacidad);
                    p.AddWithValue("costo", req.CostoEntrada);
                });

            return Results.Created($"/estadios/{nombre}/sectores/{sector}",
                new SectorResponse(sector!, req.Capacidad, req.CostoEntrada));
        });

        // GET /estadios (admin) -> lista los estadios con sus sectores.
        app.MapGet("/estadios", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var estadios = new Dictionary<string, EstadioResponse>();

            await db.QueryAsync(
                """
                SELECT e.nombre, e.direccion, s.nombre, s.capacidad, s.costo_entrada
                FROM estadio e
                LEFT JOIN sector s ON s.nombre_estadio = e.nombre
                ORDER BY e.nombre, s.nombre
                """,
                r =>
                {
                    var nombreEstadio = r.GetString(0);
                    if (!estadios.TryGetValue(nombreEstadio, out var estadio))
                    {
                        estadio = new EstadioResponse(
                            nombreEstadio,
                            r.IsDBNull(1) ? null : r.GetString(1),
                            new List<SectorResponse>());
                        estadios.Add(nombreEstadio, estadio);
                    }

                    if (!r.IsDBNull(2))
                        estadio.Sectores.Add(new SectorResponse(r.GetString(2), r.GetInt32(3), r.GetDecimal(4)));

                    return 0; // el mapeo acumula en el diccionario; el valor no se usa
                });

            return Results.Ok(estadios.Values);
        });
    }
}
