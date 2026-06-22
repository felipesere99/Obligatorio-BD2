using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Validaciones;

public static class ValidacionesEndpoints
{
    // Cadencia de rotación del QR dinámico (pista para el cliente).
    private const int QrExpiraEnSegundos = 30;

    public static void MapValidaciones(this IEndpointRouteBuilder app)
    {
        // POST /validaciones (funcionario) -> valida el ingreso de una entrada.
        // El funcionario es el usuario autenticado; el código y el dispositivo
        // vienen del cuerpo. La función verifica QR activo, dispositivo del
        // funcionario y asignación al sector, y consume la entrada.
        app.MapPost("/validaciones", async (HttpContext ctx, ValidarEntradaRequest req, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.Funcionario);
            if (error is not null) return error;

            var validacion = await db.QuerySingleAsync(
                "CALL sp_validar_entrada(@codigo, @funcionario, @dispositivo)",
                r => new ValidacionResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3),
                    r.GetDateTime(4), r.GetString(5), r.GetInt32(6)),
                p =>
                {
                    p.AddWithValue("codigo", req.Codigo);
                    p.AddWithValue("funcionario", user!.Documento);
                    p.AddWithValue("dispositivo", req.IdDispositivo);
                });

            return Results.Created($"/validaciones/{validacion!.NroEntrada}", validacion);
        });

        // POST /entradas/{id}/qr (usuario_general) -> genera/rota el QR de una
        // entrada propia. Pensado para invocarse cada ~30s. Solo el propietario.
        app.MapPost("/entradas/{id:int}/qr", async (int id, HttpContext ctx, Db db) =>
        {
            var (user, propietarioError) = await AutorizarPropietario(ctx, db, id);
            if (propietarioError is not null) return propietarioError;

            var qr = await db.QuerySingleAsync(
                "CALL sp_generar_qr(@entrada)",
                r => new QrResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDateTime(3),
                    QrExpiraEnSegundos),
                p => p.AddWithValue("entrada", id));

            return Results.Ok(qr);
        });

        // GET /entradas/{id}/qr (usuario_general) -> código QR activo actual de
        // una entrada propia (sin rotarlo). Devuelve 404 si todavía no se generó.
        app.MapGet("/entradas/{id:int}/qr", async (int id, HttpContext ctx, Db db) =>
        {
            var (user, propietarioError) = await AutorizarPropietario(ctx, db, id);
            if (propietarioError is not null) return propietarioError;

            var qr = await db.QuerySingleAsync(
                """
                SELECT id_codigo, nro_entrada, codigo, generado_en
                FROM codigo_qr
                WHERE nro_entrada = @entrada AND activo = TRUE
                """,
                r => new QrResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDateTime(3),
                    QrExpiraEnSegundos),
                p => p.AddWithValue("entrada", id));

            return qr is null
                ? Results.NotFound(new ApiError("La entrada no tiene un QR activo."))
                : Results.Ok(qr);
        });
    }

    // Autentica al usuario y verifica que sea el propietario de la entrada.
    private static async Task<(CurrentUser? User, IResult? Error)> AutorizarPropietario(
        HttpContext ctx, Db db, int nroEntrada)
    {
        var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
        if (error is not null) return (null, error);

        var propietario = await db.ScalarAsync<string>(
            "SELECT doc_propietario FROM entrada WHERE nro_entrada = @entrada",
            p => p.AddWithValue("entrada", nroEntrada));

        if (propietario is null)
            return (null, Results.NotFound(new ApiError($"No existe la entrada {nroEntrada}.")));
        if (propietario != user!.Documento)
            return (null, Results.Json(
                new ApiError("Solo podés generar el QR de tus propias entradas."), statusCode: 403));

        return (user, null);
    }
}
