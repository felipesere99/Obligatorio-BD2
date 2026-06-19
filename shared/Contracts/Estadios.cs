namespace Shared.Contracts;

/// <summary>Alta de un estadio. El nombre es la PK; la dirección es opcional.</summary>
public record RegistrarEstadioRequest(string Nombre, string? Direccion = null);

/// <summary>Alta de un sector de un estadio. El estadio viene en la ruta.</summary>
public record RegistrarSectorRequest(string Nombre, int Capacidad, decimal CostoEntrada);

/// <summary>Sector devuelto dentro de un estadio.</summary>
public record SectorResponse(string Nombre, int Capacidad, decimal CostoEntrada);

/// <summary>Estadio devuelto en el listado, con sus sectores.</summary>
public record EstadioResponse(string Nombre, string? Direccion, List<SectorResponse> Sectores);
