using MySqlConnector;
using Server.Api.Auth;
using Server.Api.Data;
using Shared.Contracts;

namespace Server.Api.Features.Estadios;

public static class EstadiosEndpoints
{
    public static void MapEstadios(this IEndpointRouteBuilder app)
    {
        // POST /estadios (admin) -> alta de un estadio.
        app.MapPost("/estadios", async (HttpContext ctx, RegistrarEstadioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre del estadio es obligatorio."));

            await db.ExecuteAsync(
                "INSERT INTO estadio(nombre, direccion) VALUES (@nombre, @direccion)",
                p =>
                {
                    p.AddWithValue("nombre", req.Nombre.Trim());
                    p.AddWithValue("direccion", (object?)req.Direccion ?? DBNull.Value);
                });

            var nombre = req.Nombre.Trim();
            return Results.Created($"/estadios/{nombre}", new { nombre });
        });

        // PUT /estadios/{nombre} (admin) -> actualiza la dirección (el nombre es PK, no se renombra).
        app.MapPut("/estadios/{nombre}", async (string nombre, HttpContext ctx, ActualizarEstadioRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            var rowsAffected = await db.ExecuteAsync(
                "UPDATE estadio SET direccion = @direccion WHERE nombre = @nombre",
                p =>
                {
                    p.AddWithValue("nombre", nombre.Trim());
                    p.AddWithValue("direccion", string.IsNullOrWhiteSpace(req.Direccion) ? DBNull.Value : req.Direccion.Trim());
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Estadio no encontrado."));

            return Results.NoContent();
        });

        // DELETE /estadios/{nombre} (admin) -> elimina el estadio (cascade borra sectores); 1451 si tiene eventos.
        app.MapDelete("/estadios/{nombre}", async (string nombre, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    "DELETE FROM estadio WHERE nombre = @nombre",
                    p => p.AddWithValue("nombre", nombre.Trim()));

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Estadio no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un estadio con eventos asociados."));
            }
        });

        // POST /estadios/{nombre}/sectores (admin) -> alta de un sector del estadio.
        app.MapPost("/estadios/{nombre}/sectores", async (string nombre, HttpContext ctx, RegistrarSectorRequest req, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Results.BadRequest(new ApiError("El nombre del sector es obligatorio."));
            if (req.Capacidad <= 0)
                return Results.BadRequest(new ApiError("La capacidad debe ser mayor a cero."));
            if (req.CostoEntrada < 0)
                return Results.BadRequest(new ApiError("El costo de entrada no puede ser negativo."));

            await db.ExecuteAsync(
                "INSERT INTO sector(nombre_estadio, nombre, capacidad, costo_entrada) VALUES (@estadio, @sectorNombre, @capacidad, @costo)",
                p =>
                {
                    p.AddWithValue("estadio", nombre);
                    p.AddWithValue("sectorNombre", req.Nombre.Trim());
                    p.AddWithValue("capacidad", req.Capacidad);
                    p.AddWithValue("costo", req.CostoEntrada);
                });

            var sector = req.Nombre.Trim();
            return Results.Created($"/estadios/{nombre}/sectores/{sector}",
                new SectorResponse(sector, req.Capacidad, req.CostoEntrada));
        });

        // PUT /estadios/{nombre}/sectores/{sector} (admin) -> actualiza capacidad y costo del sector.
        app.MapPut("/estadios/{nombre}/sectores/{sector}", async (
            string nombre,
            string sector,
            HttpContext ctx,
            ActualizarSectorRequest req,
            Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            if (req.Capacidad <= 0)
                return Results.BadRequest(new ApiError("La capacidad debe ser mayor a cero."));
            if (req.CostoEntrada < 0)
                return Results.BadRequest(new ApiError("El costo de entrada no puede ser negativo."));

            // Ningún trigger valida bajar la capacidad por debajo de lo ya vendido (el de capacidad
            // solo corre al insertar una entrada); lo guardamos acá explícitamente.
            var vendidas = await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM entrada WHERE nombre_estadio = @estadio AND nombre_sector = @sector",
                p =>
                {
                    p.AddWithValue("estadio", nombre.Trim());
                    p.AddWithValue("sector", sector.Trim());
                });

            if (req.Capacidad < vendidas)
                return Results.BadRequest(new ApiError($"La capacidad no puede ser menor a las {vendidas} entradas ya vendidas."));

            var rowsAffected = await db.ExecuteAsync(
                """
                UPDATE sector
                SET capacidad = @capacidad, costo_entrada = @costo
                WHERE nombre_estadio = @estadio AND nombre = @sector
                """,
                p =>
                {
                    p.AddWithValue("estadio", nombre.Trim());
                    p.AddWithValue("sector", sector.Trim());
                    p.AddWithValue("capacidad", req.Capacidad);
                    p.AddWithValue("costo", req.CostoEntrada);
                });

            if (rowsAffected == 0)
                return Results.NotFound(new ApiError("Sector no encontrado."));

            return Results.NoContent();
        });

        // DELETE /estadios/{nombre}/sectores/{sector} (admin) -> elimina el sector; 1451 si está referenciado.
        app.MapDelete("/estadios/{nombre}/sectores/{sector}", async (string nombre, string sector, HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador);
            if (error is not null) return error;

            try
            {
                var rowsAffected = await db.ExecuteAsync(
                    "DELETE FROM sector WHERE nombre_estadio = @estadio AND nombre = @sector",
                    p =>
                    {
                        p.AddWithValue("estadio", nombre.Trim());
                        p.AddWithValue("sector", sector.Trim());
                    });

                if (rowsAffected == 0)
                    return Results.NotFound(new ApiError("Sector no encontrado."));

                return Results.NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint
            {
                return Results.BadRequest(new ApiError("No se puede eliminar un sector habilitado en eventos, con entradas o con funcionarios asignados."));
            }
        });

        // GET /estadios (admin, usuario_general) -> lista los estadios con sus sectores.
        app.MapGet("/estadios", async (HttpContext ctx, Db db) =>
        {
            var (_, error) = ctx.Authorize(Roles.Administrador, Roles.UsuarioGeneral);
            if (error is not null) return error;

            var estadios = new Dictionary<string, EstadioResponse>();

            await db.QueryAsync(
                """
                SELECT e.nombre, e.direccion, s.nombre, s.capacidad, s.costo_entrada
                FROM estadio e
                LEFT JOIN sector s ON s.nombre_estadio = e.nombre
                ORDER BY e.nombre, s.nombre
                """,
                r =>
                {
                    var nombreEstadio = r.GetString(0);
                    if (!estadios.TryGetValue(nombreEstadio, out var estadio))
                    {
                        estadio = new EstadioResponse(
                            nombreEstadio,
                            r.IsDBNull(1) ? null : r.GetString(1),
                            new List<SectorResponse>());
                        estadios.Add(nombreEstadio, estadio);
                    }

                    if (!r.IsDBNull(2))
                        estadio.Sectores.Add(new SectorResponse(r.GetString(2), r.GetInt32(3), r.GetDecimal(4)));

                    return 0;
                });

            return Results.Ok(estadios.Values);
        });
    }
}
