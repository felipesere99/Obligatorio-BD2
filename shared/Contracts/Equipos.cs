namespace Shared.Contracts;

/// <summary>Alta de un equipo. El país es la PK; el nombre es el del seleccionado.</summary>
public record RegistrarEquipoRequest(string Pais, string Nombre);

/// <summary>Modificación de un equipo. El país es la PK (no se renombra); solo cambia el nombre.</summary>
public record ActualizarEquipoRequest(string Nombre);

/// <summary>Equipo devuelto en el listado y al registrarse.</summary>
public record EquipoResponse(string Pais, string Nombre);
