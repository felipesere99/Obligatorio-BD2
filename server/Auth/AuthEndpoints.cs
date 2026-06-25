using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        // POST /login { "documento": "UG-1", "contrasenia": "..." } -> { documento, rol, nombre }
        app.MapPost("/login", async (LoginRequest req, Db db) =>
        {
            var documento = req.Documento?.Trim() ?? string.Empty;
            var contrasenia = req.Contrasenia ?? string.Empty;

            if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(contrasenia))
                return Results.Json(new ApiError("Documento o contraseña inválidos."), statusCode: 401);

            var row = await db.QuerySingleAsync(
                """
                SELECT u.documento, u.rol, u.nombre, c.hash
                FROM (
                    SELECT documento, 'administrador' AS rol, nombre FROM administrador
                    UNION ALL
                    SELECT documento, 'funcionario'   AS rol, nombre FROM funcionario
                    UNION ALL
                    SELECT documento, 'usuario_general' AS rol, nombre FROM usuario_general
                ) u
                JOIN credencial c ON c.documento = u.documento
                WHERE u.documento = @doc
                LIMIT 1
                """,
                r => new
                {
                    Session = new UserSession(r.GetString(0), r.GetString(1), r.GetString(2)),
                    Hash = r.GetString(3),
                },
                p => p.AddWithValue("doc", documento));

            if (row is null || !PasswordHasher.Verify(contrasenia, row.Hash))
                return Results.Json(new ApiError("Documento o contraseña inválidos."), statusCode: 401);

            return Results.Ok(row.Session);
        });
    }
}
