namespace Shared.Contracts;

/// <summary>Login mínimo: se identifica por documento (el esquema no tiene password).</summary>
public record LoginRequest(string Documento);

/// <summary>Sesión devuelta por el server; el client la reenvía en cada request.</summary>
public record UserSession(string Documento, string Rol, string Nombre);

/// <summary>Nombres de rol (deben coincidir con los que devuelve fn_login).</summary>
public static class Roles
{
    public const string Administrador  = "administrador";
    public const string Funcionario    = "funcionario";
    public const string UsuarioGeneral = "usuario_general";
}
