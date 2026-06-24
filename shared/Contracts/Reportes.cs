namespace Shared.Contracts;

/// <summary>Ventas agregadas por evento.</summary>
public record ReporteEventoVentasResponse(
    int IdEvento,
    string NombreEvento,
    string NombreEstadio,
    int CantidadEntradas,
    decimal TotalVentas);

/// <summary>Ventas agregadas por sector habilitado en un evento.</summary>
public record ReporteSectorVentasResponse(
    int IdEvento,
    string NombreEvento,
    string NombreEstadio,
    string NombreSector,
    int CantidadEntradas,
    decimal TotalVentas);

/// <summary>Ranking de compradores por monto gastado.</summary>
public record ReporteCompradorResponse(
    string Documento,
    string Nombre,
    string Apellido,
    int CantidadCompras,
    int CantidadEntradas,
    decimal TotalGastado);
