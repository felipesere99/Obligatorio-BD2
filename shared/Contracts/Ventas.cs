namespace Shared.Contracts;

/// <summary>Un item de la compra: una entrada para un sector de un evento.</summary>
public record CompraItem(
    int IdEvento,
    string Estadio,
    string Sector,
    string? Fila = null,
    string? Asiento = null);

/// <summary>Compra de entradas (el comprador es el usuario autenticado).</summary>
public record CrearVentaRequest(List<CompraItem> Items);

/// <summary>Resultado de la compra.</summary>
public record VentaCreadaResponse(int NroVenta, decimal MontoTotal);

/// <summary>Una compra (venta) del usuario, con la cantidad de entradas.</summary>
public record CompraResponse(
    int NroVenta,
    decimal MontoTotal,
    string Estado,
    DateTime Fecha,
    int CantidadEntradas);

/// <summary>Una entrada de una venta.</summary>
public record EntradaResponse(
    int NroEntrada,
    int IdEvento,
    string NombreEstadio,
    string NombreSector,
    string? Fila,
    string? Asiento);
