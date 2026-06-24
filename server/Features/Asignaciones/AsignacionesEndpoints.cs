using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Asignaciones;

public static class AsignacionesEndpoints
{
    public static void MapAsignaciones(this IEndpointRouteBuilder app)
    {
        // POST /asignaciones (admin) -> asigna un funcionario a un evento y sector.
        app.MapPost("/asignaciones", async (HttpContext ctx, [FromBody] AsignarFuncionarioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.DocFuncionario))
                return Results.BadRequest(new ApiError("El funcionario es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.NombreEstadio))
                return Results.BadRequest(new ApiError("El estadio es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.NombreSector))
                return Results.BadRequest(new ApiError("El sector es obligatorio."));

            var docFuncionario = req.DocFuncionario.Trim();
            var nombreEstadio = req.NombreEstadio.Trim();
            var nombreSector = req.NombreSector.Trim();

            var sectorHabilitado = await db.ScalarAsync<long>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM evento_sector
                    WHERE id_evento = @idEvento
                      AND nombre_estadio = @nombreEstadio
                      AND nombre_sector = @nombreSector
                )
                """,
                p =>
                {
                    p.AddWithValue("idEvento", req.IdEvento);
                    p.AddWithValue("nombreEstadio", nombreEstadio);
                    p.AddWithValue("nombreSector", nombreSector);
                });

            if (sectorHabilitado != 1)
                return Results.BadRequest(new ApiError("El sector no está habilitado para este evento."));

            try
            {
                await db.ExecuteAsync(
                    """
                    INSERT INTO funcionario_asignado (doc_funcionario, id_evento, nombre_estadio, nombre_sector)
                    VALUES (@docFuncionario, @idEvento, @nombreEstadio, @nombreSector)
                    """,
                    p =>
                    {
                        p.AddWithValue("docFuncionario", docFuncionario);
                        p.AddWithValue("idEvento", req.IdEvento);
                        p.AddWithValue("nombreEstadio", nombreEstadio);
                        p.AddWithValue("nombreSector", nombreSector);
                    });

                return Results.Created(
                    $"/asignaciones/{docFuncionario}/{req.IdEvento}/{nombreEstadio}/{nombreSector}",
                    new FuncionarioAsignadoResponse(docFuncionario, req.IdEvento, nombreEstadio, nombreSector));
            }
            catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062) // Duplicate key
            {
                return Results.BadRequest(new ApiError("El funcionario ya está asignado a este evento y sector."));
            }
            catch (MySqlConnector.MySqlException ex) when (ex.Number == 1452) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("Funcionario, evento o sector no válido."));
            }
        });

        // GET /asignaciones (admin) -> lista todas las asignaciones.
        app.MapGet("/asignaciones", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var asignaciones = new List<AsignacionResponse>();

            await db.QueryAsync(
                """
                SELECT fa.doc_funcionario, f.nombre, fa.id_evento, fa.nombre_estadio, fa.nombre_sector
                FROM funcionario_asignado fa
                JOIN funcionario f ON f.documento = fa.doc_funcionario
                ORDER BY fa.id_evento, fa.nombre_estadio, fa.nombre_sector, fa.doc_funcionario
                """,
                r =>
                {
                    asignaciones.Add(new AsignacionResponse(
                        r.GetString(0),
                        r.GetString(1),
                        r.GetInt32(2),
                        r.GetString(3),
                        r.GetString(4)));
                    return 0;
                });

            return Results.Ok(asignaciones);
        });

        // GET /asignaciones/{id_evento} (admin) -> lista asignaciones de un evento.
        app.MapGet("/asignaciones/{idEvento:int}", async (int idEvento, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var asignaciones = new List<AsignacionResponse>();

            await db.QueryAsync(
                """
                SELECT fa.doc_funcionario, f.nombre, fa.id_evento, fa.nombre_estadio, fa.nombre_sector
                FROM funcionario_asignado fa
                JOIN funcionario f ON f.documento = fa.doc_funcionario
                WHERE fa.id_evento = @idEvento
                ORDER BY fa.nombre_estadio, fa.nombre_sector, fa.doc_funcionario
                """,
                r =>
                {
                    asignaciones.Add(new AsignacionResponse(
                        r.GetString(0),
                        r.GetString(1),
                        r.GetInt32(2),
                        r.GetString(3),
                        r.GetString(4)));
                    return 0;
                },
                p => p.AddWithValue("idEvento", idEvento));

            return Results.Ok(asignaciones);
        });

        // DELETE /asignaciones (admin) -> elimina la asignación de un funcionario.
        app.MapDelete("/asignaciones", async (HttpContext ctx, [FromBody] DesasignarFuncionarioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rowsAffected = await db.ExecuteAsync(
                """
                DELETE FROM funcionario_asignado
                WHERE doc_funcionario = @docFuncionario
                  AND id_evento = @idEvento
                  AND nombre_estadio = @nombreEstadio
                  AND nombre_sector = @nombreSector
                """,
                p =>
                {
                    p.AddWithValue("docFuncionario", req.DocFuncionario);
                    p.AddWithValue("idEvento", req.IdEvento);
                    p.AddWithValue("nombreEstadio", req.NombreEstadio);
                    p.AddWithValue("nombreSector", req.NombreSector);
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Asignación no encontrada."));

            return Results.NoContent();
        });
    }
}

