using MySqlConnector;
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

            if (req.Porcentaje < 0)
                return Results.BadRequest(new ApiError("El porcentaje debe ser mayor o igual a cero."));

            ComisionResponse? comision = null;
            await db.TransactionAsync(async (conn, ct) =>
            {
                await using var updCmd = conn.CreateCommand();
                updCmd.CommandText = "UPDATE comision SET vigente_hasta = NOW(6) WHERE vigente_hasta IS NULL";
                await updCmd.ExecuteNonQueryAsync(ct);

                await using var insCmd = conn.CreateCommand();
                insCmd.CommandText = "INSERT INTO comision(porcentaje, vigente_desde) VALUES (@pct, NOW(6))";
                insCmd.Parameters.AddWithValue("pct", req.Porcentaje);
                await insCmd.ExecuteNonQueryAsync(ct);
                var newId = insCmd.LastInsertedId;

                await using var selCmd = conn.CreateCommand();
                selCmd.CommandText = "SELECT id_comision, porcentaje, vigente_desde FROM comision WHERE id_comision = @id";
                selCmd.Parameters.AddWithValue("id", newId);
                await using var reader = await selCmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    comision = MapComision(reader);
            });

            return Results.Created($"/comisiones/vigente", comision);
        });

        // GET /comisiones/vigente (admin) -> la comisión vigente.
        app.MapGet("/comisiones/vigente", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var comision = await db.QuerySingleAsync(
                "SELECT id_comision, porcentaje, vigente_desde FROM comision WHERE vigente_hasta IS NULL",
                MapComision);

            return comision is null
                ? Results.NotFound(new ApiError("No hay una comisión vigente."))
                : Results.Ok(comision);
        });
    }

    private static ComisionResponse MapComision(MySqlDataReader r) =>
        new(r.GetInt32(0), r.GetDecimal(1), r.GetDateTime(2));
}
