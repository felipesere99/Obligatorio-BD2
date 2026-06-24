using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Equipos;

public static class EquiposEndpoints
{
    public static void MapEquipos(this IEndpointRouteBuilder app)
    {
        // POST /equipos (admin) -> alta de un equipo.
        app.MapPost("/equipos", async (HttpContext ctx, RegistrarEquipoRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Pais))
                return Results.BadRequest(new ApiError("El país es obligatorio."));
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre es obligatorio."));

            await db.ExecuteAsync(
                "INSERT INTO equipo(pais, nombre) VALUES (@pais, @nombre)",
                p =>
                {
                    p.AddWithValue("pais", req.Pais.Trim());
                    p.AddWithValue("nombre", req.Nombre.Trim());
                });

            var pais = req.Pais.Trim();
            return Results.Created($"/equipos/{pais}", new EquipoResponse(pais, req.Nombre.Trim()));
        });

        // GET /equipos (admin) -> lista todos los equipos.
        app.MapGet("/equipos", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                "SELECT pais, nombre FROM equipo ORDER BY pais",
                r => new EquipoResponse(r.GetString(0), r.GetString(1)));

            return Results.Ok(rows);
        });
    }
}
