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
            var doc = await db.ScalarAsync<string>(
                "SELECT fn_registrar_usuario_general(@doc, @nombre, @apellido, @correo, @pais, @localidad, @calle, @numero, @cp)",
                p =>
                {
                    p.AddWithValue("doc", req.Documento);
                    p.AddWithValue("nombre", req.Nombre);
                    p.AddWithValue("apellido", req.Apellido);
                    p.AddWithValue("correo", req.Correo);
                    p.AddWithValue("pais", (object?)req.DirPais ?? DBNull.Value);
                    p.AddWithValue("localidad", (object?)req.DirLocalidad ?? DBNull.Value);
                    p.AddWithValue("calle", (object?)req.DirCalle ?? DBNull.Value);
                    p.AddWithValue("numero", (object?)req.DirNumero ?? DBNull.Value);
                    p.AddWithValue("cp", (object?)req.DirCodigoPostal ?? DBNull.Value);
                });

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
    }
}
