using Npgsql;

namespace Server.Api.Data;

/// <summary>
/// Acceso a datos compartido. Envuelve el <see cref="NpgsqlDataSource"/> y
/// ofrece helpers para llamar funciones SQL / queries sin repetir boilerplate.
/// La lógica de negocio vive en funciones y triggers de la base (no en C#).
/// </summary>
public sealed class Db(NpgsqlDataSource dataSource)
{
    /// <summary>Ejecuta una query que devuelve un único valor escalar.</summary>
    public async Task<T?> ScalarAsync<T>(
        string sql,
        Action<NpgsqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var cmd = dataSource.CreateCommand(sql);
        bind?.Invoke(cmd.Parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? default : (T)result;
    }

    /// <summary>Ejecuta una query y mapea cada fila con <paramref name="map"/>.</summary>
    public async Task<List<T>> QueryAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        Action<NpgsqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var cmd = dataSource.CreateCommand(sql);
        bind?.Invoke(cmd.Parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await reader.ReadAsync(ct))
            list.Add(map(reader));
        return list;
    }

    /// <summary>Mapea la primera fila, o devuelve <c>default</c> si no hay filas.</summary>
    public async Task<T?> QuerySingleAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        Action<NpgsqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        var rows = await QueryAsync(sql, map, bind, ct);
        return rows.Count > 0 ? rows[0] : default;
    }

    /// <summary>Ejecuta un comando sin resultado (INSERT/UPDATE/DELETE); devuelve filas afectadas.</summary>
    public async Task<int> ExecuteAsync(
        string sql,
        Action<NpgsqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var cmd = dataSource.CreateCommand(sql);
        bind?.Invoke(cmd.Parameters);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
