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
        // GET /funcionario/dispositivos (funcionario) -> dispositivos del propio
        // funcionario, para elegir con cuál valida.
        app.MapGet("/funcionario/dispositivos", async (HttpContext ctx, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.Funcionario);
            if (error is not null) return error;

            var dispositivos = await db.QueryAsync(
                """
                SELECT id_dispositivo
                FROM funcionario_dispositivo
                WHERE doc_funcionario = @doc
                ORDER BY id_dispositivo
                """,
                r => new DispositivoFuncionarioResponse(r.GetInt32(0)),
                p => p.AddWithValue("doc", user!.Documento));

            return Results.Ok(dispositivos);
        });

        // POST /validaciones (funcionario) -> valida el ingreso de una entrada.
        // El funcionario es el usuario autenticado; el código y el dispositivo
        // vienen del cuerpo. Verifica QR activo, dispositivo del funcionario y
        // asignación al sector, registra la validación y consume la entrada.
        app.MapPost("/validaciones", async (HttpContext ctx, ValidarEntradaRequest req, Db db) =>
        {
            var (user, error) = ctx.Authorize(Roles.Funcionario);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Codigo))
                return Results.BadRequest(new ApiError("El código QR es obligatorio."));

            var doc = user!.Documento;

            // 1) El QR debe estar activo; traemos también los datos de la entrada.
            var info = await db.QuerySingleAsync(
                """
                SELECT c.id_codigo, c.nro_entrada, e.hora_validacion,
                       e.id_evento, e.nombre_estadio, e.nombre_sector
                FROM codigo_qr c
                JOIN entrada e ON e.nro_entrada = c.nro_entrada
                WHERE c.codigo = @codigo AND c.activo = TRUE
                LIMIT 1
                """,
                r => new
                {
                    IdCodigo = r.GetInt32(0),
                    NroEntrada = r.GetInt32(1),
                    Validada = r.IsDBNull(2),
                    IdEvento = r.GetInt32(3),
                    Estadio = r.GetString(4),
                    Sector = r.GetString(5),
                },
                p => p.AddWithValue("codigo", req.Codigo));

            if (info is null)
                return Results.BadRequest(new ApiError("Código QR inválido o expirado."));

            // 2) Ingreso único: la entrada no puede estar ya validada.
            if (!info.Validada)
                return Results.BadRequest(new ApiError("La entrada ya fue validada."));

            // 3) El dispositivo debe pertenecer al funcionario.
            var dispositivoDelFuncionario = await db.ScalarAsync<long>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM funcionario_dispositivo
                    WHERE doc_funcionario = @doc AND id_dispositivo = @disp
                )
                """,
                p =>
                {
                    p.AddWithValue("doc", doc);
                    p.AddWithValue("disp", req.IdDispositivo);
                });

            if (dispositivoDelFuncionario != 1)
                return Results.BadRequest(new ApiError("El dispositivo no pertenece al funcionario."));

            // 4) El funcionario debe estar asignado al sector del evento.
            var asignado = await db.ScalarAsync<long>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM funcionario_asignado
                    WHERE doc_funcionario = @doc
                      AND id_evento       = @evento
                      AND nombre_estadio  = @estadio
                      AND nombre_sector   = @sector
                )
                """,
                p =>
                {
                    p.AddWithValue("doc", doc);
                    p.AddWithValue("evento", info.IdEvento);
                    p.AddWithValue("estadio", info.Estadio);
                    p.AddWithValue("sector", info.Sector);
                });

            if (asignado != 1)
                return Results.BadRequest(
                    new ApiError("El funcionario no está asignado a ese sector del evento."));

            // Registro + consumo, atómico. La validación de la entrada es
            // irreversible (trigger tr_validacion_irreversible) y el UNIQUE de
            // validacion(nro_entrada) frena una doble validación bajo carrera.
            DateTime fechaHora = default;
            await db.TransactionAsync(async (conn, ct) =>
            {
                await using (var valCmd = conn.CreateCommand())
                {
                    valCmd.CommandText = """
                        INSERT INTO validacion(nro_entrada, id_codigo, doc_funcionario, id_dispositivo)
                        VALUES (@nro, @codigo, @doc, @disp)
                        """;
                    valCmd.Parameters.AddWithValue("nro", info.NroEntrada);
                    valCmd.Parameters.AddWithValue("codigo", info.IdCodigo);
                    valCmd.Parameters.AddWithValue("doc", doc);
                    valCmd.Parameters.AddWithValue("disp", req.IdDispositivo);
                    await valCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var entradaCmd = conn.CreateCommand())
                {
                    entradaCmd.CommandText = """
                        UPDATE entrada
                        SET hora_validacion = NOW(6), id_dispositivo = @disp
                        WHERE nro_entrada = @nro
                        """;
                    entradaCmd.Parameters.AddWithValue("disp", req.IdDispositivo);
                    entradaCmd.Parameters.AddWithValue("nro", info.NroEntrada);
                    await entradaCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var qrCmd = conn.CreateCommand())
                {
                    qrCmd.CommandText = "UPDATE codigo_qr SET activo = FALSE WHERE id_codigo = @codigo";
                    qrCmd.Parameters.AddWithValue("codigo", info.IdCodigo);
                    await qrCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var readCmd = conn.CreateCommand())
                {
                    readCmd.CommandText = "SELECT fecha_hora FROM validacion WHERE id_codigo = @codigo";
                    readCmd.Parameters.AddWithValue("codigo", info.IdCodigo);
                    fechaHora = (DateTime)(await readCmd.ExecuteScalarAsync(ct))!;
                }
            });

            var validacion = new ValidacionResponse(
                info.NroEntrada, info.IdEvento, info.Estadio, info.Sector,
                fechaHora, doc, req.IdDispositivo);

            return Results.Created($"/validaciones/{validacion.NroEntrada}", validacion);
        });

        // POST /entradas/{id}/qr (usuario_general, propietario) -> genera/rota el
        // QR de una entrada propia. Pensado para invocarse cada ~30s.
        app.MapPost("/entradas/{id:int}/qr", async (int id, HttpContext ctx, Db db) =>
        {
            var (user, propietarioError) = await AutorizarPropietario(ctx, db, id, exigirNoValidada: true);
            if (propietarioError is not null) return propietarioError;

            // Token único e impredecible para el nuevo código activo.
            var codigo = $"QR-{id}-{Guid.NewGuid():N}";

            QrResponse? qr = null;
            await db.TransactionAsync(async (conn, ct) =>
            {
                await using (var offCmd = conn.CreateCommand())
                {
                    offCmd.CommandText =
                        "UPDATE codigo_qr SET activo = FALSE WHERE nro_entrada = @nro AND activo = TRUE";
                    offCmd.Parameters.AddWithValue("nro", id);
                    await offCmd.ExecuteNonQueryAsync(ct);
                }

                long idCodigo;
                await using (var insCmd = conn.CreateCommand())
                {
                    insCmd.CommandText = """
                        INSERT INTO codigo_qr(nro_entrada, codigo, activo)
                        VALUES (@nro, @codigo, TRUE)
                        """;
                    insCmd.Parameters.AddWithValue("nro", id);
                    insCmd.Parameters.AddWithValue("codigo", codigo);
                    await insCmd.ExecuteNonQueryAsync(ct);
                    idCodigo = insCmd.LastInsertedId;
                }

                await using (var readCmd = conn.CreateCommand())
                {
                    readCmd.CommandText = "SELECT generado_en FROM codigo_qr WHERE id_codigo = @id";
                    readCmd.Parameters.AddWithValue("id", idCodigo);
                    var generadoEn = (DateTime)(await readCmd.ExecuteScalarAsync(ct))!;
                    qr = new QrResponse((int)idCodigo, id, codigo, generadoEn, QrExpiraEnSegundos);
                }
            });

            return Results.Ok(qr);
        });

        // GET /entradas/{id}/qr (usuario_general, propietario) -> código QR activo
        // actual de una entrada propia (sin rotarlo). 404 si todavía no se generó.
        app.MapGet("/entradas/{id:int}/qr", async (int id, HttpContext ctx, Db db) =>
        {
            var (_, propietarioError) = await AutorizarPropietario(ctx, db, id, exigirNoValidada: false);
            if (propietarioError is not null) return propietarioError;

            var qr = await db.QuerySingleAsync(
                """
                SELECT id_codigo, nro_entrada, codigo, generado_en
                FROM codigo_qr
                WHERE nro_entrada = @nro AND activo = TRUE
                """,
                r => new QrResponse(
                    r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDateTime(3),
                    QrExpiraEnSegundos),
                p => p.AddWithValue("nro", id));

            return qr is null
                ? Results.NotFound(new ApiError("La entrada no tiene un QR activo."))
                : Results.Ok(qr);
        });
    }

    // Autentica al usuario_general y verifica que sea el propietario de la entrada.
    // Si exigirNoValidada, además rechaza entradas ya validadas (no tiene sentido
    // generar QR para una entrada consumida).
    private static async Task<(CurrentUser? User, IResult? Error)> AutorizarPropietario(
        HttpContext ctx, Db db, int nroEntrada, bool exigirNoValidada)
    {
        var (user, error) = ctx.Authorize(Roles.UsuarioGeneral);
        if (error is not null) return (null, error);

        var entrada = await db.QuerySingleAsync(
            "SELECT doc_propietario, hora_validacion FROM entrada WHERE nro_entrada = @nro",
            r => new { Propietario = r.GetString(0), Validada = !r.IsDBNull(1) },
            p => p.AddWithValue("nro", nroEntrada));

        if (entrada is null)
            return (null, Results.NotFound(new ApiError($"No existe la entrada {nroEntrada}.")));
        if (entrada.Propietario != user!.Documento)
            return (null, Results.Json(
                new ApiError("Solo podés operar el QR de tus propias entradas."), statusCode: 403));
        if (exigirNoValidada && entrada.Validada)
            return (null, Results.BadRequest(
                new ApiError("La entrada ya fue validada: no se puede generar un QR.")));

        return (user, null);
    }
}
