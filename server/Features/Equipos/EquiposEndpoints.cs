using MySqlConnector;
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

            var pais = req.Pais.Trim();
            try
            {
                await db.ExecuteAsync(
                    "INSERT INTO equipo(pais, nombre) VALUES (@pais, @nombre)",
                    p =>
                    {
                        p.AddWithValue("pais", pais);
                        p.AddWithValue("nombre", req.Nombre.Trim());
                    });
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate key
            {
                return Results.BadRequest(new ApiError("Ya existe un equipo con ese país."));
            }

            return Results.Created($"/equipos/{pais}", new EquipoResponse(pais, req.Nombre.Trim()));
        });

        // PUT /equipos/{pais} (admin) -> actualiza el nombre del equipo (el país es PK, no se renombra).
        app.MapPut("/equipos/{pais}", async (string pais, HttpContext ctx, ActualizarEquipoRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre es obligatorio."));

            var rowsAffected = await db.ExecuteAsync(
                "UPDATE equipo SET nombre = @nombre WHERE pais = @pais",
                p =>
                {
                    p.AddWithValue("pais", pais.Trim());
                    p.AddWithValue("nombre", req.Nombre.Trim());
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Equipo no encontrado."));

            return Results.NoContent();
        });

        // DELETE /equipos/{pais} (admin) -> elimina el equipo si no tiene eventos asociados.
        app.MapDelete("/equipos/{pais}", async (string pais, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    "DELETE FROM equipo WHERE pais = @pais",
                    p => p.AddWithValue("pais", pais.Trim()));

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Equipo no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un equipo con eventos asociados."));
            }
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
