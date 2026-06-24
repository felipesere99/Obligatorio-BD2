using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Reportes;

public static class ReportesEndpoints
{
    public static void MapReportes(this IEndpointRouteBuilder app)
    {
        // GET /reportes/eventos (admin) -> top eventos por entradas vendidas y monto.
        app.MapGet("/reportes/eventos", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT ev.id_evento,
                       ev.nombre,
                       ev.nombre_estadio,
                       COUNT(e.nro_entrada) AS cantidad_entradas,
                       COALESCE(SUM(s.costo_entrada * (1 + COALESCE(c.porcentaje, 0) / 100)), 0) AS total_ventas
                FROM evento ev
                LEFT JOIN entrada e ON e.id_evento = ev.id_evento
                LEFT JOIN venta v ON v.nro_venta = e.nro_venta
                LEFT JOIN comision c ON c.id_comision = v.id_comision
                LEFT JOIN sector s ON s.nombre_estadio = e.nombre_estadio
                                  AND s.nombre = e.nombre_sector
                GROUP BY ev.id_evento, ev.nombre, ev.nombre_estadio
                ORDER BY total_ventas DESC, cantidad_entradas DESC, ev.id_evento
                """,
                r => new ReporteEventoVentasResponse(
                    r.GetInt32(0),
                    r.GetString(1),
                    r.GetString(2),
                    (int)r.GetInt64(3),
                    r.GetDecimal(4)));

            return Results.Ok(rows);
        });

        // GET /reportes/sectores (admin) -> top sectores por entradas vendidas y monto.
        app.MapGet("/reportes/sectores", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT es.id_evento,
                       ev.nombre,
                       es.nombre_estadio,
                       es.nombre_sector,
                       COUNT(e.nro_entrada) AS cantidad_entradas,
                       COALESCE(SUM(s.costo_entrada * (1 + COALESCE(c.porcentaje, 0) / 100)), 0) AS total_ventas
                FROM evento_sector es
                JOIN evento ev ON ev.id_evento = es.id_evento
                JOIN sector s ON s.nombre_estadio = es.nombre_estadio
                             AND s.nombre = es.nombre_sector
                LEFT JOIN entrada e ON e.id_evento = es.id_evento
                                   AND e.nombre_estadio = es.nombre_estadio
                                   AND e.nombre_sector = es.nombre_sector
                LEFT JOIN venta v ON v.nro_venta = e.nro_venta
                LEFT JOIN comision c ON c.id_comision = v.id_comision
                GROUP BY es.id_evento, ev.nombre, es.nombre_estadio, es.nombre_sector
                ORDER BY total_ventas DESC, cantidad_entradas DESC, es.id_evento, es.nombre_sector
                """,
                r => new ReporteSectorVentasResponse(
                    r.GetInt32(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    (int)r.GetInt64(4),
                    r.GetDecimal(5)));

            return Results.Ok(rows);
        });

        // GET /reportes/compradores (admin) -> ranking de compradores por gasto.
        app.MapGet("/reportes/compradores", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rows = await db.QueryAsync(
                """
                SELECT ug.documento,
                       ug.nombre,
                       ug.apellido,
                       COALESCE(vt.cantidad_compras, 0) AS cantidad_compras,
                       COALESCE(et.cantidad_entradas, 0) AS cantidad_entradas,
                       COALESCE(vt.total_gastado, 0) AS total_gastado
                FROM usuario_general ug
                LEFT JOIN (
                    SELECT doc_comprador,
                           COUNT(*) AS cantidad_compras,
                           SUM(monto_total) AS total_gastado
                    FROM venta
                    GROUP BY doc_comprador
                ) vt ON vt.doc_comprador = ug.documento
                LEFT JOIN (
                    SELECT v.doc_comprador,
                           COUNT(e.nro_entrada) AS cantidad_entradas
                    FROM venta v
                    JOIN entrada e ON e.nro_venta = v.nro_venta
                    GROUP BY v.doc_comprador
                ) et ON et.doc_comprador = ug.documento
                ORDER BY total_gastado DESC, cantidad_entradas DESC, ug.documento
                """,
                r => new ReporteCompradorResponse(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    (int)r.GetInt64(3),
                    (int)r.GetInt64(4),
                    r.GetDecimal(5)));

            return Results.Ok(rows);
        });
    }
}
