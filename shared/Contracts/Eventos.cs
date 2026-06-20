namespace Shared.Contracts;

/// <summary>Alta de un evento en un estadio entre dos equipos (por país).</summary>
public record CrearEventoRequest(
    string Nombre,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string PaisLocal,
    string PaisVisitante,
    string NombreEstadio);

/// <summary>Respuesta al crear un evento: id generado.</summary>
public record EventoCreadoResponse(int IdEvento);

/// <summary>Habilita un sector (de un estadio) para un evento.</summary>
public record HabilitarSectorRequest(string NombreEstadio, string NombreSector);

/// <summary>Evento devuelto en el listado, con sus sectores habilitados.</summary>
public record EventoResponse(
    int IdEvento,
    string Nombre,
    DateTime FechaInicio,
    DateTime FechaFin,
    string? PaisLocal,
    string? PaisVisitante,
    string NombreEstadio,
    List<string> SectoresHabilitados);
