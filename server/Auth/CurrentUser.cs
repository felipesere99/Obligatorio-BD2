using Shared.Contracts;

namespace Server.Api.Auth;

/// <summary>Usuario autenticado, derivado de los headers que envía el client.</summary>
public record CurrentUser(string Documento, string Rol);

public static class CurrentUserExtensions
{
    public const string DocHeader = "X-Documento";
    public const string RolHeader = "X-Rol";

    /// <summary>Lee la sesión de los headers, o null si no vienen.</summary>
    public static CurrentUser? GetUser(this HttpContext ctx)
    {
        var doc = ctx.Request.Headers[DocHeader].ToString();
        var rol = ctx.Request.Headers[RolHeader].ToString();
        return string.IsNullOrWhiteSpace(doc) || string.IsNullOrWhiteSpace(rol)
            ? null
            : new CurrentUser(doc, rol);
    }

    /// <summary>
    /// Verifica autenticación y (opcionalmente) rol. Patrón de uso en un endpoint:
    /// <code>
    /// var (user, error) = ctx.Authorize(Roles.Administrador);
    /// if (error is not null) return error;
    /// </code>
    /// </summary>
    public static (CurrentUser? User, IResult? Error) Authorize(this HttpContext ctx, params string[] roles)
    {
        var user = ctx.GetUser();
        if (user is null)
            return (null, Results.Json(new ApiError("No autenticado."), statusCode: 401));
        if (roles.Length > 0 && !roles.Contains(user.Rol))
            return (null, Results.Json(new ApiError("Rol no autorizado para esta operación."), statusCode: 403));
        return (user, null);
    }
}
