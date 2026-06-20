using System.Net;
using MySqlConnector;
using Shared.Contracts;

namespace Server.Api.Infrastructure;

/// <summary>
/// Traduce excepciones de MySQL a respuestas HTTP con cuerpo <see cref="ApiError"/>.
/// En particular, los SIGNAL SQLSTATE '45000' de procedimientos y triggers
/// (reglas de negocio) se reportan como 400 con el mensaje original.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    // Códigos de error de MySQL (https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html)
    private const int ErSignalException     = 1644; // SIGNAL SQLSTATE '45000' => regla de negocio
    private const int ErDupEntry            = 1062; // UNIQUE / PRIMARY KEY duplicado
    private const int ErNoReferencedRow     = 1452; // FK: referencia a una fila inexistente
    private const int ErRowIsReferenced     = 1451; // FK: borrar una fila aún referenciada
    private const int ErCheckConstraint     = 3819; // violación de un CHECK

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (MySqlException ex) when (ex.Number == ErSignalException)
        {
            // SIGNAL en un procedimiento/trigger => regla de negocio violada.
            logger.LogWarning("Regla de negocio: {Message}", ex.Message);
            await Write(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (MySqlException ex) when (ex.Number == ErDupEntry)
        {
            await Write(context, HttpStatusCode.Conflict, "Ya existe un registro con esos datos.");
        }
        catch (MySqlException ex) when (ex.Number is ErNoReferencedRow or ErRowIsReferenced)
        {
            await Write(context, HttpStatusCode.BadRequest, "Referencia a un registro inexistente.");
        }
        catch (MySqlException ex) when (ex.Number == ErCheckConstraint)
        {
            await Write(context, HttpStatusCode.BadRequest, "Un valor no cumple una restricción del modelo.");
        }
        catch (MySqlException ex)
        {
            logger.LogError(ex, "Error de base de datos");
            await Write(context, HttpStatusCode.ServiceUnavailable, "Base de datos no disponible.");
        }
    }

    private static async Task Write(HttpContext ctx, HttpStatusCode code, string message)
    {
        if (ctx.Response.HasStarted)
            return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)code;
        await ctx.Response.WriteAsJsonAsync(new ApiError(message));
    }
}
