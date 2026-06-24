namespace Shared.Contracts;

/// <summary>Asigna un funcionario a un evento y sector específico.</summary>
public record AsignarFuncionarioRequest(
    string DocFuncionario,
    int IdEvento,
    string NombreEstadio,
    string NombreSector);

/// <summary>Respuesta al asignar un funcionario.</summary>
public record FuncionarioAsignadoResponse(
    string DocFuncionario,
    int IdEvento,
    string NombreEstadio,
    string NombreSector);

/// <summary>Información de asignación de un funcionario.</summary>
public record AsignacionResponse(
    string DocFuncionario,
    string NombreFuncionario,
    int IdEvento,
    string NombreEstadio,
    string NombreSector);

/// <summary>Elimina la asignación de un funcionario a un evento y sector.</summary>
public record DesasignarFuncionarioRequest(
    string DocFuncionario,
    int IdEvento,
    string NombreEstadio,
    string NombreSector);
