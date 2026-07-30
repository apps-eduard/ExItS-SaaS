using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>Creates Microsoft.Data.Sqlite connections for foundation local databases.</summary>
public sealed class LocalDatabaseFactory : ILocalDatabaseFactory
{
    public Task<ILocalDatabaseConnection> OpenAsync(string databasePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return Task.FromResult<ILocalDatabaseConnection>(new SqliteLocalDatabaseConnection(connection, databasePath));
    }
}

internal sealed class SqliteLocalDatabaseConnection(SqliteConnection connection, string databasePath)
    : ILocalDatabaseConnection
{
    public string DatabasePath { get; } = databasePath;

    public async Task ExecuteAsync(string sql, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<T?> QueryScalarAsync<T>(string sql, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (result is null or DBNull)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryRowsAsync(
        string sql,
        CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public ValueTask DisposeAsync()
    {
        connection.Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose() => connection.Dispose();
}
