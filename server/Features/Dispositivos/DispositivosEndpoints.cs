using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Dispositivos;

public static class DispositivosEndpoints
{
    public static void MapDispositivos(this IEndpointRouteBuilder app)
    {
        // GET /dispositivos (admin) -> lista dispositivos y funcionarios vinculados.
        app.MapGet("/dispositivos", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT d.id_dispositivo,
                       d.nro_serie,
                       d.marca,
                       d.modelo,
                       d.habilitado,
                       COALESCE(GROUP_CONCAT(fd.doc_funcionario ORDER BY fd.doc_funcionario SEPARATOR ','), '') AS funcionarios
                FROM dispositivo d
                LEFT JOIN funcionario_dispositivo fd ON fd.id_dispositivo = d.id_dispositivo
                GROUP BY d.id_dispositivo
                ORDER BY d.id_dispositivo
                """,
                r =>
                {
                    var funcionarios = r.GetString(5);
                    return new DispositivoResponse(
                        r.GetInt32(0),
                        r.GetString(1),
                        r.GetString(2),
                        r.GetString(3),
                        r.GetBoolean(4),
                        string.IsNullOrWhiteSpace(funcionarios)
                            ? []
                            : funcionarios.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                });

            return Results.Ok(rows);
        });

        // POST /dispositivos (admin) -> crea un dispositivo validador.
        app.MapPost("/dispositivos", async (HttpContext ctx, [FromBody] GuardarDispositivoRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var validation = ValidateDispositivo(req);
            if (validation is not null) return validation;

            var nroSerie = req.NroSerie.Trim();
            try
            {
                var id = await db.InsertAsync(
                    """
                    INSERT INTO dispositivo(nro_serie, marca, modelo, habilitado)
                    VALUES (@nroSerie, @marca, @modelo, @habilitado)
                    """,
                    p =>
                    {
                        p.AddWithValue("nroSerie", nroSerie);
                        p.AddWithValue("marca", req.Marca.Trim());
                        p.AddWithValue("modelo", req.Modelo.Trim());
                        p.AddWithValue("habilitado", req.Habilitado);
                    });

                return Results.Created($"/dispositivos/{id}", new DispositivoCreadoResponse((int)id, nroSerie));
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate key
            {
                return Results.BadRequest(new ApiError("Ya existe un dispositivo con ese numero de serie."));
            }
        });

        // PUT /dispositivos/{id} (admin) -> actualiza los datos del dispositivo.
        app.MapPut("/dispositivos/{idDispositivo:int}", async (
            int idDispositivo,
            HttpContext ctx,
            [FromBody] GuardarDispositivoRequest req,
            Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var validation = ValidateDispositivo(req);
            if (validation is not null) return validation;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    """
                    UPDATE dispositivo
                    SET nro_serie = @nroSerie,
                        marca = @marca,
                        modelo = @modelo,
                        habilitado = @habilitado
                    WHERE id_dispositivo = @idDispositivo
                    """,
                    p =>
                    {
                        p.AddWithValue("idDispositivo", idDispositivo);
                        p.AddWithValue("nroSerie", req.NroSerie.Trim());
                        p.AddWithValue("marca", req.Marca.Trim());
                        p.AddWithValue("modelo", req.Modelo.Trim());
                        p.AddWithValue("habilitado", req.Habilitado);
                    });

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Dispositivo no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate key
            {
                return Results.BadRequest(new ApiError("Ya existe un dispositivo con ese numero de serie."));
            }
        });

        // DELETE /dispositivos/{id} (admin) -> elimina un dispositivo si no esta en uso.
        app.MapDelete("/dispositivos/{idDispositivo:int}", async (int idDispositivo, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    "DELETE FROM dispositivo WHERE id_dispositivo = @idDispositivo",
                    p => p.AddWithValue("idDispositivo", idDispositivo));

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Dispositivo no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un dispositivo asignado o usado en validaciones."));
            }
        });

        // GET /funcionarios/{doc}/dispositivos (admin) -> lista dispositivos de un funcionario.
        app.MapGet("/funcionarios/{docFuncionario}/dispositivos", async (string docFuncionario, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var doc = docFuncionario.Trim();
            var existe = await db.ScalarAsync<long>(
                "SELECT EXISTS(SELECT 1 FROM funcionario WHERE documento = @docFuncionario)",
                p => p.AddWithValue("docFuncionario", doc));

            if (existe != 1)
                return Results.NotFound(new ApiError("Funcionario no encontrado."));

            var rows = await db.QueryAsync(
                """
                SELECT d.id_dispositivo, d.nro_serie, d.marca, d.modelo, d.habilitado
                FROM funcionario_dispositivo fd
                JOIN dispositivo d ON d.id_dispositivo = fd.id_dispositivo
                WHERE fd.doc_funcionario = @docFuncionario
                ORDER BY d.id_dispositivo
                """,
                r => new DispositivoResponse(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetBoolean(4), [doc]),
                p => p.AddWithValue("docFuncionario", doc));

            return Results.Ok(rows);
        });

        // POST /funcionarios/{doc}/dispositivos (admin) -> vincula un dispositivo al funcionario.
        app.MapPost("/funcionarios/{docFuncionario}/dispositivos", async (
            string docFuncionario,
            HttpContext ctx,
            [FromBody] AsignarDispositivoRequest req,
            Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (req.IdDispositivo <= 0)
                return Results.BadRequest(new ApiError("El dispositivo es obligatorio."));

            var doc = docFuncionario.Trim();
            if (string.IsNullOrWhiteSpace(doc))
                return Results.BadRequest(new ApiError("El funcionario es obligatorio."));

            try
            {
                await db.ExecuteAsync(
                    """
                    INSERT INTO funcionario_dispositivo(doc_funcionario, id_dispositivo)
                    VALUES (@docFuncionario, @idDispositivo)
                    """,
                    p =>
                    {
                        p.AddWithValue("docFuncionario", doc);
                        p.AddWithValue("idDispositivo", req.IdDispositivo);
                    });

                return Results.Created(
                    $"/funcionarios/{doc}/dispositivos/{req.IdDispositivo}",
                    new DispositivoAsignadoResponse(doc, req.IdDispositivo));
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate key
            {
                return Results.BadRequest(new ApiError("El funcionario ya tiene asignado ese dispositivo."));
            }
            catch (MySqlException ex) when (ex.Number == 1452) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("Funcionario o dispositivo no valido."));
            }
        });

        // DELETE /funcionarios/{doc}/dispositivos/{id} (admin) -> quita el vinculo.
        app.MapDelete("/funcionarios/{docFuncionario}/dispositivos/{idDispositivo:int}", async (
            string docFuncionario,
            int idDispositivo,
            HttpContext ctx,
            Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rowsAffected = await db.ExecuteAsync(
                """
                DELETE FROM funcionario_dispositivo
                WHERE doc_funcionario = @docFuncionario
                  AND id_dispositivo = @idDispositivo
                """,
                p =>
                {
                    p.AddWithValue("docFuncionario", docFuncionario.Trim());
                    p.AddWithValue("idDispositivo", idDispositivo);
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Asignacion de dispositivo no encontrada."));

            return Results.NoContent();
        });
    }

    private static IResult? ValidateDispositivo(GuardarDispositivoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NroSerie))
            return Results.BadRequest(new ApiError("El numero de serie es obligatorio."));
        if (string.IsNullOrWhiteSpace(req.Marca))
            return Results.BadRequest(new ApiError("La marca es obligatoria."));
        if (string.IsNullOrWhiteSpace(req.Modelo))
            return Results.BadRequest(new ApiError("El modelo es obligatorio."));
        return null;
    }
}
