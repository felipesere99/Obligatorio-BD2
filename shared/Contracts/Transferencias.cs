namespace Shared.Contracts;

/// <summary>
/// Inicia una transferencia de entrada. El emisor es el usuario autenticado;
/// los triggers de la BD validan tenencia, límites y estado de la entrada.
/// </summary>
public record IniciarTransferenciaRequest(int NroEntrada, string DocReceptor);

/// <summary>
/// Resuelve una transferencia pendiente. <see cref="Accion"/> debe ser
/// "aceptar", "rechazar" o "cancelar".
/// </summary>
public record ResolverTransferenciaRequest(string Accion);

/// <summary>Una transferencia (histórico o pendiente).</summary>
public record TransferenciaResponse(
    int NroEntrada,
    DateTime FechaHora,
    int Contador,
    string DocEmisor,
    string DocReceptor,
    string Estado);

/// <summary>Entrada que posee actualmente un usuario (vista de tenencia).</summary>
public record EntradaTenenciaResponse(
    int NroEntrada,
    int IdEvento,
    string NombreEstadio,
    string NombreSector,
    string? Fila,
    string? Asiento,
    DateTime? HoraValidacion);
