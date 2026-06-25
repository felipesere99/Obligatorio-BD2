namespace Shared.Contracts;

/// <summary>Dispositivo de validacion de entradas.</summary>
public record DispositivoResponse(
    int IdDispositivo,
    string NroSerie,
    string Marca,
    string Modelo,
    bool Habilitado,
    string[] FuncionariosAsignados);

/// <summary>Respuesta al crear un dispositivo.</summary>
public record DispositivoCreadoResponse(
    int IdDispositivo,
    string NroSerie);

/// <summary>Datos editables de un dispositivo validador.</summary>
public record GuardarDispositivoRequest(
    string NroSerie,
    string Marca,
    string Modelo,
    bool Habilitado);

/// <summary>Asigna un dispositivo a un funcionario.</summary>
public record AsignarDispositivoRequest(int IdDispositivo);

/// <summary>Respuesta al asignar un dispositivo a un funcionario.</summary>
public record DispositivoAsignadoResponse(
    string DocFuncionario,
    int IdDispositivo);
