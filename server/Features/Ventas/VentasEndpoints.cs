using System.Text.Json;
using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Ventas;

public static class VentasEndpoints
{
    public static void MapVentas(this IEndpointRouteBuilder app)
    {
        // POST /ventas (usuario_general) -> compra entradas. El comprador es
        // el usuario autenticado; nunca se toma del cuerpo.
        app.MapPost("/ventas", async (HttpContext ctx, CrearVentaRequest req, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
            if (error is not null) return error;

            // Serializamos los items con las claves que espera la función SQL.
            var itemsJson = JsonSerializer.Serialize(req.Items.Select(i => new
            {
                id_evento = i.IdEvento,
                estadio = i.Estadio,
                sector = i.Sector,
                fila = i.Fila,
                asiento = i.Asiento,
            }));

            var venta = await db.QuerySingleAsync(
                "SELECT * FROM fn_crear_venta(@comprador, @items::jsonb)",
                r => new VentaCreadaResponse(r.GetInt32(0), r.GetDecimal(1)),
                p =>
                {
                    p.AddWithValue("comprador", user!.Documento);
                    p.AddWithValue("items", itemsJson);
                });

            return Results.Created($"/ventas/{venta!.NroVenta}", venta);
        });

        // GET /usuarios/{doc}/compras -> compras del propio usuario.
        app.MapGet("/usuarios/{doc}/compras", async (string doc, HttpContext ctx, Db db) =>
        {
            var (user, error) = ctx.Authorize();
            if (error is not null) return error;
            if (user!.Documento != doc)
                return Results.Json(new ApiError("Solo podés ver tus propias compras."), statusCode: 403);

            var compras = await db.QueryAsync(
                """
                SELECT v.nro_venta, v.monto_total, v.estado, v.fecha, count(e.nro_entrada)
                FROM venta v
                LEFT JOIN entrada e ON e.nro_venta = v.nro_venta
                WHERE v.doc_comprador = @doc
                GROUP BY v.nro_venta, v.monto_total, v.estado, v.fecha
                ORDER BY v.nro_venta
                """,
                r => new CompraResponse(
                    r.GetInt32(0), r.GetDecimal(1), r.GetString(2),
                    r.GetFieldValue<DateTimeOffset>(3), (int)r.GetInt64(4)),
                p => p.AddWithValue("doc", doc));

            return Results.Ok(compras);
        });

        // GET /ventas/{nro}/entradas -> entradas de una venta (solo su dueño).
        app.MapGet("/ventas/{nro:int}/entradas", async (int nro, HttpContext ctx, Db db) =>
        {
            var (user, error) = ctx.Authorize();
            if (error is not null) return error;

            var comprador = await db.ScalarAsync<string>(
                "SELECT doc_comprador FROM venta WHERE nro_venta = @nro",
                p => p.AddWithValue("nro", nro));

            if (comprador is null)
                return Results.NotFound(new ApiError($"No existe la venta {nro}."));
            if (comprador != user!.Documento)
                return Results.Json(new ApiError("Solo podés ver las entradas de tus propias compras."), statusCode: 403);

            var entradas = await db.QueryAsync(
                """
                SELECT nro_entrada, id_evento, nombre_estadio, nombre_sector, fila, asiento
                FROM entrada
                WHERE nro_venta = @nro
                ORDER BY nro_entrada
                """,
                r => new EntradaResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3),
                    r.IsDBNull(4) ? null : r.GetString(4),
                    r.IsDBNull(5) ? null : r.GetString(5)),
                p => p.AddWithValue("nro", nro));

            return Results.Ok(entradas);
        });
    }
}
