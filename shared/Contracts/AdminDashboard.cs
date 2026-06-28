namespace Shared.Contracts;

/// <summary>Resumen operativo para la pantalla inicial del administrador.</summary>
public record AdminDashboardResponse(
    AdminDashboardTotalesResponse Totales,
    List<AdminDashboardEventoVentasResponse> VentasPorEvento,
    List<AdminDashboardFuncionariosEventoResponse> FuncionariosPorEvento,
    AdminDashboardDistribucionResponse Dispositivos,
    AdminDashboardDistribucionResponse Usuarios);

public record AdminDashboardTotalesResponse(
    int EventosTotales,
    int EventosProximos,
    int Funcionarios,
    int FuncionariosDisponibles,
    int Usuarios,
    int DispositivosHabilitados,
    int EntradasVendidas,
    int EntradasValidadas,
    decimal Ingresos,
    decimal TotalComisiones);

public record AdminDashboardEventoVentasResponse(
    int IdEvento,
    string NombreEvento,
    int CantidadEntradas,
    decimal TotalVentas);

public record AdminDashboardFuncionariosEventoResponse(
    int IdEvento,
    string NombreEvento,
    int CantidadFuncionarios);

public record AdminDashboardDistribucionResponse(
    string EtiquetaPrincipal,
    int ValorPrincipal,
    string EtiquetaSecundaria,
    int ValorSecundario);
