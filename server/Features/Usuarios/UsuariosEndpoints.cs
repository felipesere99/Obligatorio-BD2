using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Usuarios;

public static class UsuariosEndpoints
{
    public static void MapUsuarios(this IEndpointRouteBuilder app)
    {
        // POST /usuarios/generales (público) -> registra un usuario general.
        app.MapPost("/usuarios/generales", async (RegistrarUsuarioRequest req, Db db) =>
        {
            var documento = req.Documento?.Trim() ?? string.Empty;
            var nombre = req.Nombre?.Trim() ?? string.Empty;
            var apellido = req.Apellido?.Trim() ?? string.Empty;
            var correo = req.Correo?.Trim() ?? string.Empty;
            var contrasenia = req.Contrasenia ?? string.Empty;

            if (string.IsNullOrWhiteSpace(documento))
                return Results.BadRequest(new ApiError("El documento es obligatorio."));
            if (string.IsNullOrWhiteSpace(nombre))
                return Results.BadRequest(new ApiError("El nombre es obligatorio."));
            if (string.IsNullOrWhiteSpace(apellido))
                return Results.BadRequest(new ApiError("El apellido es obligatorio."));
            if (string.IsNullOrWhiteSpace(correo))
                return Results.BadRequest(new ApiError("El correo es obligatorio."));
            if (string.IsNullOrWhiteSpace(contrasenia))
                return Results.BadRequest(new ApiError("La contraseña es obligatoria."));
            if (contrasenia.Length < 8)
                return Results.BadRequest(new ApiError("La contraseña debe tener al menos 8 caracteres."));

            var hash = PasswordHasher.Hash(contrasenia);

            await db.TransactionAsync(async (conn, ct) =>
            {
                await using (var userCmd = conn.CreateCommand())
                {
                    userCmd.CommandText =
                        """
                        INSERT INTO usuario_general(
                            documento, nombre, apellido, correo,
                            dir_pais, dir_localidad, dir_calle, dir_numero, dir_codigo_postal
                        ) VALUES (@doc, @nombre, @apellido, @correo, @pais, @localidad, @calle, @numero, @cp)
                        """;
                    userCmd.Parameters.AddWithValue("doc", documento);
                    userCmd.Parameters.AddWithValue("nombre", nombre);
                    userCmd.Parameters.AddWithValue("apellido", apellido);
                    userCmd.Parameters.AddWithValue("correo", correo);
                    userCmd.Parameters.AddWithValue("pais", (object?)req.DirPais ?? DBNull.Value);
                    userCmd.Parameters.AddWithValue("localidad", (object?)req.DirLocalidad ?? DBNull.Value);
                    userCmd.Parameters.AddWithValue("calle", (object?)req.DirCalle ?? DBNull.Value);
                    userCmd.Parameters.AddWithValue("numero", (object?)req.DirNumero ?? DBNull.Value);
                    userCmd.Parameters.AddWithValue("cp", (object?)req.DirCodigoPostal ?? DBNull.Value);
                    await userCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var credCmd = conn.CreateCommand())
                {
                    credCmd.CommandText =
                        """
                        INSERT INTO credencial(documento, hash)
                        VALUES (@doc, @hash)
                        """;
                    credCmd.Parameters.AddWithValue("doc", documento);
                    credCmd.Parameters.AddWithValue("hash", hash);
                    await credCmd.ExecuteNonQueryAsync(ct);
                }
            });

            return Results.Created($"/usuarios/generales/{documento}", new { documento });
        });

        // GET /usuarios/generales (admin) -> lista todos los usuarios generales.
        app.MapGet("/usuarios/generales", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT documento, nombre, apellido, correo, estado_verificacion
                FROM usuario_general
                ORDER BY documento
                """,
                r => new UsuarioGeneralResponse(
                    r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetBoolean(4)));

            return Results.Ok(rows);
        });

        // GET /usuarios/funcionarios (admin) -> lista todos los funcionarios.
        app.MapGet("/usuarios/funcionarios", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT documento, nombre, apellido, correo, nro_legajo
                FROM funcionario
                ORDER BY documento
                """,
                r => new FuncionarioResponse(
                    r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4)));

            return Results.Ok(rows);
        });

        // POST /usuarios/funcionarios (admin) -> crea un funcionario con credencial.
        app.MapPost("/usuarios/funcionarios", async (HttpContext ctx, [FromBody] CrearFuncionarioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var validation = ValidateFuncionario(req.Documento, req.Nombre, req.Apellido, req.Correo, req.NroLegajo, req.Contrasenia);
            if (validation is not null) return validation;

            var hash = PasswordHasher.Hash(req.Contrasenia);
            var doc = req.Documento.Trim();

            try
            {
                await db.TransactionAsync(async (conn, ct) =>
                {
                    await using (var funcCmd = conn.CreateCommand())
                    {
                        funcCmd.CommandText =
                            """
                            INSERT INTO funcionario(
                                documento, nombre, apellido, correo, nro_legajo,
                                dir_pais, dir_localidad, dir_calle, dir_numero, dir_codigo_postal
                            ) VALUES (@doc, @nombre, @apellido, @correo, @legajo,
                                      @pais, @localidad, @calle, @numero, @cp)
                            """;
                        funcCmd.Parameters.AddWithValue("doc", doc);
                        funcCmd.Parameters.AddWithValue("nombre", req.Nombre.Trim());
                        funcCmd.Parameters.AddWithValue("apellido", req.Apellido.Trim());
                        funcCmd.Parameters.AddWithValue("correo", req.Correo.Trim());
                        funcCmd.Parameters.AddWithValue("legajo", req.NroLegajo.Trim());
                        funcCmd.Parameters.AddWithValue("pais", (object?)req.DirPais ?? DBNull.Value);
                        funcCmd.Parameters.AddWithValue("localidad", (object?)req.DirLocalidad ?? DBNull.Value);
                        funcCmd.Parameters.AddWithValue("calle", (object?)req.DirCalle ?? DBNull.Value);
                        funcCmd.Parameters.AddWithValue("numero", (object?)req.DirNumero ?? DBNull.Value);
                        funcCmd.Parameters.AddWithValue("cp", (object?)req.DirCodigoPostal ?? DBNull.Value);
                        await funcCmd.ExecuteNonQueryAsync(ct);
                    }

                    await using (var credCmd = conn.CreateCommand())
                    {
                        credCmd.CommandText = "INSERT INTO credencial(documento, hash) VALUES (@doc, @hash)";
                        credCmd.Parameters.AddWithValue("doc", doc);
                        credCmd.Parameters.AddWithValue("hash", hash);
                        await credCmd.ExecuteNonQueryAsync(ct);
                    }
                });

                return Results.Created($"/usuarios/funcionarios/{doc}", new { documento = doc });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.BadRequest(new ApiError("Ya existe un funcionario con ese documento, correo o nro. de legajo."));
            }
        });

        // PUT /usuarios/funcionarios/{documento} (admin) -> actualiza datos de un funcionario.
        app.MapPut("/usuarios/funcionarios/{documento}", async (
            string documento,
            HttpContext ctx,
            [FromBody] ActualizarFuncionarioRequest req,
            Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var validation = ValidateFuncionario(null, req.Nombre, req.Apellido, req.Correo, req.NroLegajo);
            if (validation is not null) return validation;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    """
                    UPDATE funcionario
                    SET nombre = @nombre,
                        apellido = @apellido,
                        correo = @correo,
                        nro_legajo = @legajo,
                        dir_pais = @pais,
                        dir_localidad = @localidad,
                        dir_calle = @calle,
                        dir_numero = @numero,
                        dir_codigo_postal = @cp
                    WHERE documento = @doc
                    """,
                    p =>
                    {
                        p.AddWithValue("doc", documento.Trim());
                        p.AddWithValue("nombre", req.Nombre.Trim());
                        p.AddWithValue("apellido", req.Apellido.Trim());
                        p.AddWithValue("correo", req.Correo.Trim());
                        p.AddWithValue("legajo", req.NroLegajo.Trim());
                        p.AddWithValue("pais", (object?)req.DirPais ?? DBNull.Value);
                        p.AddWithValue("localidad", (object?)req.DirLocalidad ?? DBNull.Value);
                        p.AddWithValue("calle", (object?)req.DirCalle ?? DBNull.Value);
                        p.AddWithValue("numero", (object?)req.DirNumero ?? DBNull.Value);
                        p.AddWithValue("cp", (object?)req.DirCodigoPostal ?? DBNull.Value);
                    });

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Funcionario no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.BadRequest(new ApiError("Ya existe un funcionario con ese correo o nro. de legajo."));
            }
        });

        // DELETE /usuarios/funcionarios/{documento} (admin) -> elimina un funcionario si no tiene dependencias.
        app.MapDelete("/usuarios/funcionarios/{documento}", async (string documento, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var doc = documento.Trim();

            var existe = await db.ScalarAsync<long>(
                "SELECT EXISTS(SELECT 1 FROM funcionario WHERE documento = @doc)",
                p => p.AddWithValue("doc", doc));

            if (existe != 1)
                return Results.NotFound(new ApiError("Funcionario no encontrado."));

            try
            {
                await db.TransactionAsync(async (conn, ct) =>
                {
                    await using (var credCmd = conn.CreateCommand())
                    {
                        credCmd.CommandText = "DELETE FROM credencial WHERE documento = @doc";
                        credCmd.Parameters.AddWithValue("doc", doc);
                        await credCmd.ExecuteNonQueryAsync(ct);
                    }

                    await using (var funcCmd = conn.CreateCommand())
                    {
                        funcCmd.CommandText = "DELETE FROM funcionario WHERE documento = @doc";
                        funcCmd.Parameters.AddWithValue("doc", doc);
                        await funcCmd.ExecuteNonQueryAsync(ct);
                    }
                });

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un funcionario con asignaciones, dispositivos o validaciones activos."));
            }
        });
    }

    private static IResult? ValidateFuncionario(
        string? documento, string nombre, string apellido, string correo, string nroLegajo,
        string? contrasenia = null)
    {
        if (documento is not null && string.IsNullOrWhiteSpace(documento))
            return Results.BadRequest(new ApiError("El documento es obligatorio."));
        if (string.IsNullOrWhiteSpace(nombre))
            return Results.BadRequest(new ApiError("El nombre es obligatorio."));
        if (string.IsNullOrWhiteSpace(apellido))
            return Results.BadRequest(new ApiError("El apellido es obligatorio."));
        if (string.IsNullOrWhiteSpace(correo))
            return Results.BadRequest(new ApiError("El correo es obligatorio."));
        if (string.IsNullOrWhiteSpace(nroLegajo))
            return Results.BadRequest(new ApiError("El nro. de legajo es obligatorio."));
        if (contrasenia is not null)
        {
            if (string.IsNullOrWhiteSpace(contrasenia))
                return Results.BadRequest(new ApiError("La contraseña es obligatoria."));
            if (contrasenia.Length < 8)
                return Results.BadRequest(new ApiError("La contraseña debe tener al menos 8 caracteres."));
        }
        return null;
    }
}
