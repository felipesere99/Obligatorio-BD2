using MySqlConnector;
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

            if (req.Items is null || req.Items.Count == 0)
                return Results.BadRequest(new ApiError("La compra no tiene entradas."));
            if (req.Items.Count > 5)
                return Results.BadRequest(new ApiError("Una venta admite como máximo 5 entradas."));

            // Verificar comisión vigente antes de abrir la transacción.
            var comision = await db.QuerySingleAsync(
                "SELECT id_comision, porcentaje FROM comision WHERE vigente_hasta IS NULL LIMIT 1",
                r => new { Id = r.GetInt32(0), Pct = r.GetDecimal(1) });

            if (comision is null)
                return Results.BadRequest(new ApiError("No hay una comisión vigente."));

            VentaCreadaResponse? venta = null;
            await db.TransactionAsync(async (conn, ct) =>
            {
                // Calcular monto: suma de costos de cada sector × (1 + comisión%).
                decimal subtotal = 0;
                foreach (var item in req.Items)
                {
                    await using var costoCmd = conn.CreateCommand();
                    costoCmd.CommandText =
                        "SELECT costo_entrada FROM sector WHERE nombre_estadio = @estadio AND nombre = @sector";
                    costoCmd.Parameters.AddWithValue("estadio", item.Estadio);
                    costoCmd.Parameters.AddWithValue("sector", item.Sector);
                    var costo = await costoCmd.ExecuteScalarAsync(ct);
                    subtotal += costo is null or DBNull ? 0m : (decimal)costo;
                }
                var montoTotal = Math.Round(subtotal * (1 + comision.Pct / 100), 2);

                // Insertar cabecera de la venta.
                await using var ventaCmd = conn.CreateCommand();
                ventaCmd.CommandText = """
                    INSERT INTO venta(monto_total, estado, doc_comprador, id_comision)
                    VALUES (@monto, 'paga', @comprador, @comision)
                    """;
                ventaCmd.Parameters.AddWithValue("monto", montoTotal);
                ventaCmd.Parameters.AddWithValue("comprador", user!.Documento);
                ventaCmd.Parameters.AddWithValue("comision", comision.Id);
                await ventaCmd.ExecuteNonQueryAsync(ct);
                var nroVenta = (int)ventaCmd.LastInsertedId;

                // Insertar una entrada por item; los triggers validan sector habilitado,
                // capacidad y máx 5 por venta.
                foreach (var item in req.Items)
                {
                    await using var entradaCmd = conn.CreateCommand();
                    entradaCmd.CommandText = """
                        INSERT INTO entrada(nro_venta, id_evento, nombre_estadio, nombre_sector,
                                           fila, asiento, doc_propietario)
                        VALUES (@nroVenta, @evento, @estadio, @sector, @fila, @asiento, @propietario)
                        """;
                    entradaCmd.Parameters.AddWithValue("nroVenta", nroVenta);
                    entradaCmd.Parameters.AddWithValue("evento", item.IdEvento);
                    entradaCmd.Parameters.AddWithValue("estadio", item.Estadio);
                    entradaCmd.Parameters.AddWithValue("sector", item.Sector);
                    entradaCmd.Parameters.AddWithValue("fila", (object?)item.Fila ?? DBNull.Value);
                    entradaCmd.Parameters.AddWithValue("asiento", (object?)item.Asiento ?? DBNull.Value);
                    entradaCmd.Parameters.AddWithValue("propietario", user!.Documento);
                    await entradaCmd.ExecuteNonQueryAsync(ct);
                }

                venta = new VentaCreadaResponse(nroVenta, montoTotal);
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
                    r.GetDateTime(3), (int)r.GetInt64(4)),
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
