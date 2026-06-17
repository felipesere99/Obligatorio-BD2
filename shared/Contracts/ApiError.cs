namespace Shared.Contracts;

/// <summary>Respuesta uniforme de error (incluye el mensaje de RAISE EXCEPTION del SQL).</summary>
public record ApiError(string Error);
