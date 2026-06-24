using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Transferencias;

public static class TransferenciasEndpoints
{
    public static void MapTransferencias(this IEndpointRouteBuilder app)
    {
        // POST /transferencias (usuario_general) -> inicia una transferencia.
        // El emisor es el usuario autenticado; los triggers validan límites y estado.
        app.MapPost("/transferencias", async (HttpContext ctx, IniciarTransferenciaRequest req, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.DocReceptor))
                return Results.BadRequest(new ApiError("El documento del receptor es obligatorio."));

            var doc = user!.Documento;
            if (req.DocReceptor == doc)
                return Results.BadRequest(new ApiError("No podés transferirte una entrada a vos mismo."));

            var tiene = await db.ScalarAsync<long>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM usuario_tiene_entradas
                    WHERE documento_usuario = @doc AND nro_entrada = @nro
                )
                """,
                p =>
                {
                    p.AddWithValue("doc", doc);
                    p.AddWithValue("nro", req.NroEntrada);
                });

            if (tiene != 1)
                return Results.Json(
                    new ApiError("Solo podés transferir entradas que te pertenecen."), statusCode: 403);

            await db.ExecuteAsync(
                """
                INSERT INTO transferencia(nro_entrada, doc_emisor, doc_receptor)
                VALUES (@nro, @emisor, @receptor)
                """,
                p =>
                {
                    p.AddWithValue("nro", req.NroEntrada);
                    p.AddWithValue("emisor", doc);
                    p.AddWithValue("receptor", req.DocReceptor);
                });

            var transferencia = await db.QuerySingleAsync(
                """
                SELECT nro_entrada, fecha_hora, contador, doc_emisor, doc_receptor, estado
                FROM transferencia
                WHERE nro_entrada = @nro
                ORDER BY fecha_hora DESC
                LIMIT 1
                """,
                MapTransferencia,
                p => p.AddWithValue("nro", req.NroEntrada));

            return Results.Created($"/transferencias/{transferencia!.NroEntrada}", transferencia);
        });

        // PATCH /transferencias/{nroEntrada} (usuario_general) -> acepta, rechaza o cancela
        // la transferencia pendiente de esa entrada.
        app.MapPatch("/transferencias/{nroEntrada:int}", async (
            int nroEntrada, HttpContext ctx, ResolverTransferenciaRequest req, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
            if (error is not null) return error;

            var nuevoEstado = MapAccionToEstado(req.Accion);
            if (nuevoEstado is null)
                return Results.BadRequest(new ApiError("Acción inválida. Use aceptar, rechazar o cancelar."));

            var pendiente = await db.QuerySingleAsync(
                """
                SELECT nro_entrada, fecha_hora, contador, doc_emisor, doc_receptor, estado
                FROM transferencia
                WHERE nro_entrada = @nro AND estado = 'pendiente'
                ORDER BY fecha_hora DESC
                LIMIT 1
                """,
                MapTransferencia,
                p => p.AddWithValue("nro", nroEntrada));

            if (pendiente is null)
                return Results.NotFound(new ApiError($"No hay transferencia pendiente para la entrada {nroEntrada}."));

            var doc = user!.Documento;
            var esReceptor = doc == pendiente.DocReceptor;
            var esEmisor = doc == pendiente.DocEmisor;

            if (nuevoEstado is "aceptada" or "rechazada")
            {
                if (!esReceptor)
                    return Results.Json(
                        new ApiError("Solo el receptor puede aceptar o rechazar la transferencia."),
                        statusCode: 403);
            }
            else if (!esEmisor)
            {
                return Results.Json(
                    new ApiError("Solo el emisor puede cancelar la transferencia."), statusCode: 403);
            }

            var affected = await db.ExecuteAsync(
                """
                UPDATE transferencia
                SET estado = @estado
                WHERE nro_entrada = @nro AND fecha_hora = @fecha AND estado = 'pendiente'
                """,
                p =>
                {
                    p.AddWithValue("estado", nuevoEstado);
                    p.AddWithValue("nro", nroEntrada);
                    p.AddWithValue("fecha", pendiente.FechaHora);
                });

            if (affected == 0)
                return Results.Conflict(new ApiError("La transferencia ya fue resuelta."));

            var actualizada = pendiente with { Estado = nuevoEstado };
            return Results.Ok(actualizada);
        });

        // GET /usuarios/{doc}/transferencias -> transferencias donde el usuario es emisor o receptor.
        app.MapGet("/usuarios/{doc}/transferencias", async (string doc, HttpContext ctx, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
            if (error is not null) return error;
            if (user!.Documento != doc)
                return Results.Json(new ApiError("Solo podés ver tus propias transferencias."), statusCode: 403);

            var transferencias = await db.QueryAsync(
                """
                SELECT nro_entrada, fecha_hora, contador, doc_emisor, doc_receptor, estado
                FROM transferencia
                WHERE doc_emisor = @doc OR doc_receptor = @doc
                ORDER BY fecha_hora DESC
                """,
                MapTransferencia,
                p => p.AddWithValue("doc", doc));

            return Results.Ok(transferencias);
        });

        // GET /usuarios/{doc}/entradas -> entradas que posee actualmente el usuario.
        app.MapGet("/usuarios/{doc}/entradas", async (string doc, HttpContext ctx, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
            if (error is not null) return error;
            if (user!.Documento != doc)
                return Results.Json(new ApiError("Solo podés ver tus propias entradas."), statusCode: 403);

            var entradas = await db.QueryAsync(
                """
                SELECT u.nro_entrada, e.id_evento, e.nombre_estadio, e.nombre_sector, e.fila, e.asiento
                FROM usuario_tiene_entradas u
                JOIN entrada e ON e.nro_entrada = u.nro_entrada
                WHERE u.documento_usuario = @doc
                ORDER BY u.nro_entrada
                """,
                r => new EntradaTenenciaResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3),
                    r.IsDBNull(4) ? null : r.GetString(4),
                    r.IsDBNull(5) ? null : r.GetString(5)),
                p => p.AddWithValue("doc", doc));

            return Results.Ok(entradas);
        });
    }

    private static TransferenciaResponse MapTransferencia(MySqlConnector.MySqlDataReader r) =>
        new(r.GetInt32(0), r.GetDateTime(1), r.GetInt32(2), r.GetString(3), r.GetString(4), r.GetString(5));

    private static string? MapAccionToEstado(string accion) =>
        accion.Trim().ToLowerInvariant() switch
        {
            "aceptar" => "aceptada",
            "rechazar" => "rechazada",
            "cancelar" => "cancelada",
            _ => null,
        };
}
