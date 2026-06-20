using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Comisiones;

public static class ComisionesEndpoints
{
    public static void MapComisiones(this IEndpointRouteBuilder app)
    {
        // POST /comisiones (admin) -> cierra la vigente y abre una nueva.
        app.MapPost("/comisiones", async (HttpContext ctx, SetComisionRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var comision = await db.QuerySingleAsync(
                "CALL sp_set_comision(@pct)",
                MapComision,
                p => p.AddWithValue("pct", req.Porcentaje));

            return Results.Created($"/comisiones/vigente", comision);
        });

        // GET /comisiones/vigente (admin) -> la comisión vigente.
        app.MapGet("/comisiones/vigente", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var comision = await db.QuerySingleAsync(
                "CALL sp_comision_vigente()",
                MapComision);

            return comision is null
                ? Results.NotFound(new ApiError("No hay una comisión vigente."))
                : Results.Ok(comision);
        });
    }

    private static ComisionResponse MapComision(MySqlConnector.MySqlDataReader r) =>
        new(r.GetInt32(0), r.GetDecimal(1), r.GetDateTime(2));
}
