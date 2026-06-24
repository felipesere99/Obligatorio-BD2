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
            if (string.IsNullOrWhiteSpace(req.Documento))
                return Results.BadRequest(new ApiError("El documento es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.Apellido))
                return Results.BadRequest(new ApiError("El apellido es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.Correo))
                return Results.BadRequest(new ApiError("El correo es obligatorio."));

            await db.ExecuteAsync(
                """
                INSERT INTO usuario_general(
                    documento, nombre, apellido, correo,
                    dir_pais, dir_localidad, dir_calle, dir_numero, dir_codigo_postal
                ) VALUES (@doc, @nombre, @apellido, @correo, @pais, @localidad, @calle, @numero, @cp)
                """,
                p =>
                {
                    p.AddWithValue("doc", req.Documento.Trim());
                    p.AddWithValue("nombre", req.Nombre.Trim());
                    p.AddWithValue("apellido", req.Apellido.Trim());
                    p.AddWithValue("correo", req.Correo.Trim());
                    p.AddWithValue("pais", (object?)req.DirPais ?? DBNull.Value);
                    p.AddWithValue("localidad", (object?)req.DirLocalidad ?? DBNull.Value);
                    p.AddWithValue("calle", (object?)req.DirCalle ?? DBNull.Value);
                    p.AddWithValue("numero", (object?)req.DirNumero ?? DBNull.Value);
                    p.AddWithValue("cp", (object?)req.DirCodigoPostal ?? DBNull.Value);
                });

            var doc = req.Documento.Trim();
            return Results.Created($"/usuarios/generales/{doc}", new { documento = doc });
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
