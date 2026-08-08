using System.Collections.Concurrent;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Single active local context. One initialization/migration at a time per context hash.
/// Closes on logout/org switch; preserves foundation files (no business data in P7-WP01).
/// </summary>
public sealed class LocalContextManager(
    ILocalDatabasePathResolver pathResolver,
    ILocalDatabaseFactory databaseFactory,
    ILocalDatabaseMigrator migrator,
    TimeProvider? timeProvider = null) : ILocalContextManager, IAsyncDisposable
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _activeGate = new(1, 1);
    private ILocalDatabaseConnection? _activeConnection;
    private LocalContextSnapshot? _active;

    public LocalContextSnapshot? ActiveContext => _active;

    public Task<LocalContextOpenResult> OpenPersonalAsync(Guid userId, CancellationToken ct = default) =>
        OpenCoreAsync(
            userId,
            PersonalLocalScope.PathIsolationMarker,
            PersonalLocalScope.ProductCode,
            allowPersonalMarker: true,
            ct);

    public Task<LocalContextOpenResult> OpenAsync(
        Guid userId,
        Guid organizationId,
        string productCode,
        CancellationToken ct = default)
    {
        if (organizationId == PersonalLocalScope.PathIsolationMarker)
        {
            return Task.FromResult(new LocalContextOpenResult(false, ErrorCode: "use_open_personal"));
        }

        return OpenCoreAsync(userId, organizationId, productCode, allowPersonalMarker: false, ct);
    }

    private async Task<LocalContextOpenResult> OpenCoreAsync(
        Guid userId,
        Guid organizationId,
        string productCode,
        bool allowPersonalMarker,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        if (userId == Guid.Empty || organizationId == Guid.Empty)
        {
            return new LocalContextOpenResult(false, ErrorCode: "invalid_context");
        }

        if (!allowPersonalMarker && organizationId == PersonalLocalScope.PathIsolationMarker)
        {
            return new LocalContextOpenResult(false, ErrorCode: "use_open_personal");
        }

        var normalizedProduct = string.IsNullOrWhiteSpace(productCode)
            ? PosProductCodes.PinoyBusinessPos
            : productCode.Trim();

        var hash = pathResolver.ComputeContextHash(userId, organizationId, normalizedProduct);
        var gate = _gates.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _activeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_active is not null
                    && string.Equals(_active.Identity.ContextHash, hash, StringComparison.Ordinal)
                    && _active.Status == LocalContextInitStatus.Ready
                    && _activeConnection is not null)
                {
                    return new LocalContextOpenResult(true, _active);
                }

                await CloseActiveUnlockedAsync().ConfigureAwait(false);

                var path = pathResolver.ResolveDatabasePath(userId, organizationId, normalizedProduct);
                var fileName = pathResolver.ResolveDatabaseFileName(userId, organizationId, normalizedProduct);
                var identity = new LocalContextIdentity(hash, userId, organizationId, normalizedProduct);

                ILocalDatabaseConnection connection;
                try
                {
                    connection = await databaseFactory.OpenAsync(path, ct).ConfigureAwait(false);
                }
                catch
                {
                    TryDeleteCorruptFile(path);
                    try
                    {
                        connection = await databaseFactory.OpenAsync(path, ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        _active = new LocalContextSnapshot(
                            identity,
                            fileName,
                            0,
                            _clock.GetUtcNow(),
                            LocalContextInitStatus.Failed);
                        return new LocalContextOpenResult(false, _active, "database_unavailable");
                    }
                }

                var migration = await migrator.MigrateAsync(connection, identity, ct).ConfigureAwait(false);
                if (!migration.Succeeded)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    TryDeleteCorruptFile(path);

                    try
                    {
                        connection = await databaseFactory.OpenAsync(path, ct).ConfigureAwait(false);
                        migration = await migrator.MigrateAsync(connection, identity, ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        _active = new LocalContextSnapshot(
                            identity,
                            fileName,
                            0,
                            _clock.GetUtcNow(),
                            LocalContextInitStatus.Failed);
                        return new LocalContextOpenResult(false, _active, migration.ErrorCode ?? "migration_failed");
                    }

                    if (!migration.Succeeded)
                    {
                        await connection.DisposeAsync().ConfigureAwait(false);
                        _active = new LocalContextSnapshot(
                            identity,
                            fileName,
                            migration.SchemaVersion,
                            _clock.GetUtcNow(),
                            LocalContextInitStatus.Failed);
                        return new LocalContextOpenResult(false, _active, migration.ErrorCode ?? "migration_failed");
                    }
                }

                _activeConnection = connection;
                _active = new LocalContextSnapshot(
                    identity,
                    fileName,
                    migration.SchemaVersion,
                    _clock.GetUtcNow(),
                    LocalContextInitStatus.Ready);
                return new LocalContextOpenResult(true, _active);
            }
            finally
            {
                _activeGate.Release();
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task CloseAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _activeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await CloseActiveUnlockedAsync().ConfigureAwait(false);
        }
        finally
        {
            _activeGate.Release();
        }
    }

    private async Task CloseActiveUnlockedAsync()
    {
        if (_activeConnection is not null)
        {
            await _activeConnection.DisposeAsync().ConfigureAwait(false);
            _activeConnection = null;
        }

        if (_active is not null)
        {
            _active = _active with { Status = LocalContextInitStatus.Closed };
            _active = null;
        }
    }

    private static void TryDeleteCorruptFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort recovery only.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
        _activeGate.Dispose();
        foreach (var gate in _gates.Values)
        {
            gate.Dispose();
        }
    }
}
