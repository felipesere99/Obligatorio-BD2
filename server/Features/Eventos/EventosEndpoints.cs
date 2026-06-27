using MySqlConnector;
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

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre del evento es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.NombreEstadio))
                return Results.BadRequest(new ApiError("El estadio es obligatorio."));

            var id = await db.InsertAsync(
                """
                INSERT INTO evento(nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio)
                VALUES (@nombre, @inicio, @fin, @local, @visitante, @estadio)
                """,
                p =>
                {
                    p.AddWithValue("nombre", req.Nombre.Trim());
                    p.AddWithValue("inicio", req.FechaInicio);
                    p.AddWithValue("fin", req.FechaFin);
                    p.AddWithValue("local", req.PaisLocal);
                    p.AddWithValue("visitante", req.PaisVisitante);
                    p.AddWithValue("estadio", req.NombreEstadio.Trim());
                });

            return Results.Created($"/eventos/{id}", new EventoCreadoResponse((int)id));
        });

        // PUT /eventos/{id} (admin) -> actualiza datos del evento (los triggers _upd revalidan solapes).
        app.MapPut("/eventos/{id:int}", async (int id, HttpContext ctx, ActualizarEventoRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre del evento es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.NombreEstadio))
                return Results.BadRequest(new ApiError("El estadio es obligatorio."));

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    """
                    UPDATE evento
                    SET nombre = @nombre,
                        fecha_inicio = @inicio,
                        fecha_fin = @fin,
                        pais_local = @local,
                        pais_visitante = @visitante,
                        nombre_estadio = @estadio
                    WHERE id_evento = @id
                    """,
                    p =>
                    {
                        p.AddWithValue("id", id);
                        p.AddWithValue("nombre", req.Nombre.Trim());
                        p.AddWithValue("inicio", req.FechaInicio);
                        p.AddWithValue("fin", req.FechaFin);
                        p.AddWithValue("local", req.PaisLocal);
                        p.AddWithValue("visitante", req.PaisVisitante);
                        p.AddWithValue("estadio", req.NombreEstadio.Trim());
                    });

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Evento no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1452) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("Equipo o estadio no válido."));
            }
        });

        // DELETE /eventos/{id} (admin) -> elimina el evento (cascade borra evento_sector); 1451 si tiene entradas.
        app.MapDelete("/eventos/{id:int}", async (int id, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    "DELETE FROM evento WHERE id_evento = @id",
                    p => p.AddWithValue("id", id));

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Evento no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un evento con entradas vendidas."));
            }
        });

        // POST /eventos/{id}/sectores (admin) -> habilita un sector para el evento.
        app.MapPost("/eventos/{id:int}/sectores", async (int id, HttpContext ctx, HabilitarSectorRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.NombreEstadio))
                return Results.BadRequest(new ApiError("El estadio es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.NombreSector))
                return Results.BadRequest(new ApiError("El sector es obligatorio."));

            await db.ExecuteAsync(
                "INSERT INTO evento_sector(id_evento, nombre_estadio, nombre_sector) VALUES (@evento, @estadio, @sector)",
                p =>
                {
                    p.AddWithValue("evento", id);
                    p.AddWithValue("estadio", req.NombreEstadio.Trim());
                    p.AddWithValue("sector", req.NombreSector.Trim());
                });

            var sector = req.NombreSector.Trim();
            return Results.Created($"/eventos/{id}/sectores/{sector}",
                new { idEvento = id, nombreEstadio = req.NombreEstadio, nombreSector = sector });
        });

        // DELETE /eventos/{id}/sectores/{sector} (admin) -> deshabilita un sector del evento.
        app.MapDelete("/eventos/{id:int}/sectores/{sector}", async (int id, string sector, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var nombreEstadio = await db.ScalarAsync<string?>(
                "SELECT nombre_estadio FROM evento WHERE id_evento = @id",
                p => p.AddWithValue("id", id));

            if (nombreEstadio is null)
                return Results.NotFound(new ApiError("Evento no encontrado."));

            var rowsAffected = await db.ExecuteAsync(
                """
                DELETE FROM evento_sector
                WHERE id_evento = @id AND nombre_estadio = @estadio AND nombre_sector = @sector
                """,
                p =>
                {
                    p.AddWithValue("id", id);
                    p.AddWithValue("estadio", nombreEstadio);
                    p.AddWithValue("sector", sector);
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Sector no habilitado en este evento."));

            return Results.NoContent();
        });

        // GET /eventos (admin, usuario_general) -> lista los eventos con sus sectores habilitados.
        app.MapGet("/eventos", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador, Roles.UsuarioGeneral);
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

                    return 0;
                });

            return Results.Ok(eventos.Values);
        });

        // GET /eventos/{idEvento}/disponibilidad (admin, usuario_general) -> cupos por sector habilitado.
        app.MapGet("/eventos/{idEvento:int}/disponibilidad", async (int idEvento, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador, Roles.UsuarioGeneral);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT es.nombre_sector,
                       es.nombre_estadio,
                       s.capacidad,
                       COUNT(e.nro_entrada) AS vendidas,
                       s.costo_entrada
                FROM evento_sector es
                JOIN sector s
                  ON s.nombre_estadio = es.nombre_estadio AND s.nombre = es.nombre_sector
                LEFT JOIN entrada e
                  ON e.id_evento = es.id_evento
                 AND e.nombre_estadio = es.nombre_estadio
                 AND e.nombre_sector = es.nombre_sector
                WHERE es.id_evento = @idEvento
                GROUP BY es.nombre_sector, es.nombre_estadio, s.capacidad, s.costo_entrada
                ORDER BY es.nombre_sector
                """,
                r =>
                {
                    var capacidad = r.GetInt32(2);
                    var vendidas = (int)r.GetInt64(3);
                    return new SectorDisponibilidadResponse(
                        r.GetString(0),
                        r.GetString(1),
                        capacidad,
                        vendidas,
                        capacidad - vendidas,
                        r.GetDecimal(4));
                },
                p => p.AddWithValue("idEvento", idEvento));

            return Results.Ok(rows);
        });
    }
}
