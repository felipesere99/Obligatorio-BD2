using MySqlConnector;

namespace Server.Api.Data;

/// <summary>
/// Acceso a datos compartido. Envuelve el <see cref="MySqlDataSource"/> y
/// ofrece helpers para llamar procedimientos / queries SQL sin repetir boilerplate.
/// La lógica de negocio vive en procedimientos y triggers de la base (no en C#).
/// </summary>
public sealed class Db(MySqlDataSource dataSource)
{
    /// <summary>Ejecuta una query que devuelve un único valor escalar.</summary>
    public async Task<T?> ScalarAsync<T>(
        string sql,
        Action<MySqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        bind?.Invoke(cmd.Parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? default : (T)result;
    }

    /// <summary>Ejecuta una query y mapea cada fila con <paramref name="map"/>.</summary>
    public async Task<List<T>> QueryAsync<T>(
        string sql,
        Func<MySqlDataReader, T> map,
        Action<MySqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
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
        Func<MySqlDataReader, T> map,
        Action<MySqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        var rows = await QueryAsync(sql, map, bind, ct);
        return rows.Count > 0 ? rows[0] : default;
    }

    /// <summary>Ejecuta un comando sin resultado (INSERT/UPDATE/DELETE); devuelve filas afectadas.</summary>
    public async Task<int> ExecuteAsync(
        string sql,
        Action<MySqlParameterCollection>? bind = null,
        CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        bind?.Invoke(cmd.Parameters);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Ejecuta <paramref name="work"/> dentro de una transacción: abre conexión,
    /// BEGIN, invoca el delegate (pasándole la conexión abierta), COMMIT.
    /// Si <paramref name="work"/> lanza, hace ROLLBACK y re-lanza.
    /// </summary>
    public async Task TransactionAsync(
        Func<MySqlConnection, CancellationToken, Task> work,
        CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await work(conn, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
