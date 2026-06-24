using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        // POST /login { "documento": "UG-1" } -> { documento, rol, nombre }
        app.MapPost("/login", async (LoginRequest req, Db db) =>
        {
            var rows = await db.QueryAsync(
                """
                SELECT documento, 'administrador'   AS rol, nombre FROM administrador   WHERE documento = @doc
                UNION ALL
                SELECT documento, 'funcionario',           nombre FROM funcionario     WHERE documento = @doc
                UNION ALL
                SELECT documento, 'usuario_general',       nombre FROM usuario_general WHERE documento = @doc
                """,
                r => new UserSession(r.GetString(0), r.GetString(1), r.GetString(2)),
                p => p.AddWithValue("doc", req.Documento));

            if (rows.Count == 0)
                return Results.BadRequest(new ApiError("No existe un usuario con ese documento."));

            return Results.Ok(rows[0]);
        });
    }
}
