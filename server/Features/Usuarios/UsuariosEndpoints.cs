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
    }
}
