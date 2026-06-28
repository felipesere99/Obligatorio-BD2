using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.AdminDashboard;

public static class AdminDashboardEndpoints
{
    public static void MapAdminDashboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/dashboard", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var totales = await db.QuerySingleAsync(
                """
                SELECT
                    (SELECT COUNT(*) FROM evento) AS eventos_totales,
                    (SELECT COUNT(*) FROM evento WHERE fecha_fin >= NOW(6)) AS eventos_proximos,
                    (SELECT COUNT(*) FROM funcionario) AS funcionarios,
                    (SELECT COUNT(*) FROM funcionario f
                     WHERE NOT EXISTS (
                         SELECT 1 FROM funcionario_asignado fa
                         WHERE fa.doc_funcionario = f.documento
                     )) AS funcionarios_disponibles,
                    (SELECT COUNT(*) FROM usuario_general) AS usuarios,
                    (SELECT COUNT(*) FROM dispositivo WHERE habilitado = TRUE) AS dispositivos_habilitados,
                    (SELECT COUNT(*) FROM entrada) AS entradas_vendidas,
                    (SELECT COUNT(*) FROM validacion) AS entradas_validadas,
                    (SELECT COALESCE(SUM(monto_total), 0) FROM venta) AS ingresos,
                    (SELECT COALESCE(SUM(v.monto_total), 0)
                            * COALESCE((SELECT porcentaje
                                        FROM comision
                                        WHERE vigente_hasta IS NULL
                                        ORDER BY vigente_desde DESC
                                        LIMIT 1), 0) / 100
                     FROM venta v) AS total_comisiones
                """,
                r => new AdminDashboardTotalesResponse(
                    (int)r.GetInt64(0),
                    (int)r.GetInt64(1),
                    (int)r.GetInt64(2),
                    (int)r.GetInt64(3),
                    (int)r.GetInt64(4),
                    (int)r.GetInt64(5),
                    (int)r.GetInt64(6),
                    (int)r.GetInt64(7),
                    r.GetDecimal(8),
                    r.GetDecimal(9)));

            var ventasPorEvento = await db.QueryAsync(
                """
                SELECT ev.id_evento,
                       ev.nombre,
                       COUNT(e.nro_entrada) AS cantidad_entradas,
                       COALESCE(SUM(s.costo_entrada * (1 + COALESCE(c.porcentaje, 0) / 100)), 0) AS total_ventas
                FROM evento ev
                LEFT JOIN entrada e ON e.id_evento = ev.id_evento
                LEFT JOIN venta v ON v.nro_venta = e.nro_venta
                LEFT JOIN comision c ON c.id_comision = v.id_comision
                LEFT JOIN sector s ON s.nombre_estadio = e.nombre_estadio
                                  AND s.nombre = e.nombre_sector
                GROUP BY ev.id_evento, ev.nombre
                ORDER BY cantidad_entradas DESC, total_ventas DESC, ev.id_evento
                LIMIT 6
                """,
                r => new AdminDashboardEventoVentasResponse(
                    r.GetInt32(0),
                    r.GetString(1),
                    (int)r.GetInt64(2),
                    r.GetDecimal(3)));

            var funcionariosPorEvento = await db.QueryAsync(
                """
                SELECT ev.id_evento,
                       ev.nombre,
                       COUNT(DISTINCT fa.doc_funcionario) AS cantidad_funcionarios
                FROM evento ev
                LEFT JOIN funcionario_asignado fa ON fa.id_evento = ev.id_evento
                GROUP BY ev.id_evento, ev.nombre
                ORDER BY cantidad_funcionarios DESC, ev.id_evento
                LIMIT 6
                """,
                r => new AdminDashboardFuncionariosEventoResponse(
                    r.GetInt32(0),
                    r.GetString(1),
                    (int)r.GetInt64(2)));

            var dispositivos = await db.QuerySingleAsync(
                """
                SELECT
                    COALESCE(SUM(CASE WHEN habilitado = TRUE THEN 1 ELSE 0 END), 0) AS habilitados,
                    COALESCE(SUM(CASE WHEN habilitado = FALSE THEN 1 ELSE 0 END), 0) AS deshabilitados
                FROM dispositivo
                """,
                r => new AdminDashboardDistribucionResponse(
                    "Habilitados",
                    Convert.ToInt32(r.GetValue(0)),
                    "Deshabilitados",
                    Convert.ToInt32(r.GetValue(1))));

            var usuarios = await db.QuerySingleAsync(
                """
                SELECT
                    COALESCE(SUM(CASE WHEN estado_verificacion = TRUE THEN 1 ELSE 0 END), 0) AS verificados,
                    COALESCE(SUM(CASE WHEN estado_verificacion = FALSE THEN 1 ELSE 0 END), 0) AS pendientes
                FROM usuario_general
                """,
                r => new AdminDashboardDistribucionResponse(
                    "Verificados",
                    Convert.ToInt32(r.GetValue(0)),
                    "Pendientes",
                    Convert.ToInt32(r.GetValue(1))));

            return Results.Ok(new AdminDashboardResponse(
                totales!,
                ventasPorEvento,
                funcionariosPorEvento,
                dispositivos!,
                usuarios!));
        });
    }
}
