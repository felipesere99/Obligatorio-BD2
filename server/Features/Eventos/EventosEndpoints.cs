using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Eventos;

public static class EventosEndpoints
{
    public static void MapEventos(this IEndpointRouteBuilder app)
    {
        // POST /eventos (admin) -> alta de un evento.
        app.MapPost("/eventos", async (HttpContext ctx, CrearEventoRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var id = await db.ScalarAsync<int>(
                "CALL sp_crear_evento(@nombre, @inicio, @fin, @local, @visitante, @estadio)",
                p =>
                {
                    p.AddWithValue("nombre", req.Nombre);
                    // DATETIME guarda en UTC por convención: normalizamos cualquier offset del client.
                    p.AddWithValue("inicio", req.FechaInicio.UtcDateTime);
                    p.AddWithValue("fin", req.FechaFin.UtcDateTime);
                    p.AddWithValue("local", req.PaisLocal);
                    p.AddWithValue("visitante", req.PaisVisitante);
                    p.AddWithValue("estadio", req.NombreEstadio);
                });

            return Results.Created($"/eventos/{id}", new EventoCreadoResponse(id));
        });

        // POST /eventos/{id}/sectores (admin) -> habilita un sector para el evento.
        app.MapPost("/eventos/{id:int}/sectores", async (int id, HttpContext ctx, HabilitarSectorRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var sector = await db.ScalarAsync<string>(
                "CALL sp_habilitar_sector(@evento, @estadio, @sector)",
                p =>
                {
                    p.AddWithValue("evento", id);
                    p.AddWithValue("estadio", req.NombreEstadio);
                    p.AddWithValue("sector", req.NombreSector);
                });

            return Results.Created($"/eventos/{id}/sectores/{sector}",
                new { idEvento = id, nombreEstadio = req.NombreEstadio, nombreSector = sector });
        });

        // GET /eventos (admin) -> lista los eventos con sus sectores habilitados.
        app.MapGet("/eventos", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var eventos = new Dictionary<int, EventoResponse>();

            await db.QueryAsync(
                """
                SELECT e.id_evento, e.nombre, e.fecha_inicio, e.fecha_fin,
                       e.pais_local, e.pais_visitante, e.nombre_estadio,
                       es.nombre_sector
                FROM evento e
                LEFT JOIN evento_sector es ON es.id_evento = e.id_evento
                ORDER BY e.id_evento, es.nombre_sector
                """,
                r =>
                {
                    var idEvento = r.GetInt32(0);
                    if (!eventos.TryGetValue(idEvento, out var evento))
                    {
                        evento = new EventoResponse(
                            idEvento,
                            r.GetString(1),
                            r.GetDateTime(2),
                            r.GetDateTime(3),
                            r.IsDBNull(4) ? null : r.GetString(4),
                            r.IsDBNull(5) ? null : r.GetString(5),
                            r.GetString(6),
                            new List<string>());
                        eventos.Add(idEvento, evento);
                    }

                    if (!r.IsDBNull(7))
                        evento.SectoresHabilitados.Add(r.GetString(7));

                    return 0; // el mapeo acumula en el diccionario; el valor no se usa
                });

            return Results.Ok(eventos.Values);
        });
    }
}
