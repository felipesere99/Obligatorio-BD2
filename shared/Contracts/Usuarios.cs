namespace Shared.Contracts;

/// <summary>Alta de un usuario general (registro público). La dirección es opcional.</summary>
public record RegistrarUsuarioRequest(
    string Documento,
    string Nombre,
    string Apellido,
    string Correo,
    string Contrasenia,
    string? DirPais = null,
    string? DirLocalidad = null,
    string? DirCalle = null,
    string? DirNumero = null,
    string? DirCodigoPostal = null);

/// <summary>Respuesta al registrar: documento del usuario recién creado.</summary>
public record UsuarioRegistradoResponse(string Documento);

/// <summary>Usuario general devuelto en el listado (admin).</summary>
public record UsuarioGeneralResponse(
    string Documento,
    string Nombre,
    string Apellido,
    string Correo,
    bool EstadoVerificacion);

/// <summary>Funcionario devuelto en el listado (admin).</summary>
public record FuncionarioResponse(
    string Documento,
    string Nombre,
    string Apellido,
    string Correo,
    string NroLegajo);

/// <summary>Alta de un funcionario (admin).</summary>
public record CrearFuncionarioRequest(
    string Documento,
    string Nombre,
    string Apellido,
    string Correo,
    string Contrasenia,
    string NroLegajo,
    string? DirPais = null,
    string? DirLocalidad = null,
    string? DirCalle = null,
    string? DirNumero = null,
    string? DirCodigoPostal = null);

/// <summary>Modificación de un funcionario (admin).</summary>
public record ActualizarFuncionarioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string NroLegajo,
    string? DirPais = null,
    string? DirLocalidad = null,
    string? DirCalle = null,
    string? DirNumero = null,
    string? DirCodigoPostal = null);
