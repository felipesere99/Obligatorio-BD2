namespace Shared.Contracts;

/// <summary>Setea una nueva comisión vigente (cierra la anterior).</summary>
public record SetComisionRequest(decimal Porcentaje);

/// <summary>Comisión devuelta (la vigente o la recién creada).</summary>
public record ComisionResponse(int IdComision, decimal Porcentaje, DateTimeOffset VigenteDesde);
