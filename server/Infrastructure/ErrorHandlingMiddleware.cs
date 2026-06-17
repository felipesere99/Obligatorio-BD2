using System.Net;
using Npgsql;
using Shared.Contracts;

namespace Server.Api.Infrastructure;

/// <summary>
/// Traduce excepciones de Postgres a respuestas HTTP con cuerpo <see cref="ApiError"/>.
/// En particular, los RAISE EXCEPTION de funciones y triggers (reglas de negocio)
/// se reportan como 400 con el mensaje original.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.RaiseException)
        {
            // RAISE EXCEPTION en una función/trigger => regla de negocio violada.
            logger.LogWarning("Regla de negocio: {Message}", ex.MessageText);
            await Write(context, HttpStatusCode.BadRequest, ex.MessageText);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await Write(context, HttpStatusCode.Conflict, "Ya existe un registro con esos datos.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await Write(context, HttpStatusCode.BadRequest, "Referencia a un registro inexistente.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
        {
            await Write(context, HttpStatusCode.BadRequest, "Un valor no cumple una restricción del modelo.");
        }
        catch (NpgsqlException ex)
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
