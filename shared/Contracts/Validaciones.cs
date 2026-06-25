namespace Shared.Contracts;

/// <summary>
/// Pedido de validación de ingreso: el funcionario escanea el código QR de una
/// entrada con su dispositivo. El funcionario es el usuario autenticado.
/// </summary>
public record ValidarEntradaRequest(string Codigo, int IdDispositivo);

/// <summary>Resultado de una validación de ingreso exitosa.</summary>
public record ValidacionResponse(
    int NroEntrada,
    int IdEvento,
    string NombreEstadio,
    string NombreSector,
    DateTime FechaHora,
    string DocFuncionario,
    int IdDispositivo);

/// <summary>Un dispositivo de validación asociado a un funcionario.</summary>
public record DispositivoFuncionarioResponse(int IdDispositivo);

/// <summary>
/// Código QR activo de una entrada. <see cref="ExpiraEnSegundos"/> es una pista
/// para el cliente: cada cuánto debería volver a pedir un código fresco.
/// </summary>
public record QrResponse(
    int IdCodigo,
    int NroEntrada,
    string Codigo,
    DateTime GeneradoEn,
    int ExpiraEnSegundos);
