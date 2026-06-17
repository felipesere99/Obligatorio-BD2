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
                "SELECT documento, rol, nombre FROM fn_login(@doc)",
                r => new UserSession(r.GetString(0), r.GetString(1), r.GetString(2)),
                p => p.AddWithValue("doc", req.Documento));

            // fn_login lanza excepción si no existe (la atrapa el middleware -> 400).
            return Results.Ok(rows[0]);
        });
    }
}
