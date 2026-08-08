using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Stable local installation identity. Not authentication or authorization proof.
/// Persisted via <see cref="ISecureTokenStore"/>; regenerated only when secure storage is lost.
/// </summary>
public interface IDeviceIdentityProvider
{
    /// <summary>Returns a non-empty DeviceId, creating and persisting one on first use.</summary>
    Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default);
}

/// <summary>Resolves sandboxed SQLite file paths for a user/organization/product context.</summary>
public interface ILocalDatabasePathResolver
{
    /// <summary>Deterministic hashed context key (hex) used for isolation and filenames.</summary>
    string ComputeContextHash(Guid userId, Guid organizationId, string productCode);

    /// <summary>Absolute path to the SQLite file. Filename never contains raw user/org IDs.</summary>
    string ResolveDatabasePath(Guid userId, Guid organizationId, string productCode);

    /// <summary>Hashed filename only (no directory), safe for diagnostics.</summary>
    string ResolveDatabaseFileName(Guid userId, Guid organizationId, string productCode);
}

/// <summary>Provides the application-sandbox directory for local databases.</summary>
public interface ILocalStoreRootPathProvider
{
    string GetLocalStoreRootDirectory();
}

/// <summary>Opens SQLite connections for a resolved path with safe defaults.</summary>
public interface ILocalDatabaseFactory
{
    Task<ILocalDatabaseConnection> OpenAsync(string databasePath, CancellationToken ct = default);
}

/// <summary>Disposable SQLite connection wrapper. Callers must not expose SQL details to UI.</summary>
public interface ILocalDatabaseConnection : IAsyncDisposable, IDisposable
{
    string DatabasePath { get; }
    Task ExecuteAsync(string sql, CancellationToken ct = default);
    Task<T?> QueryScalarAsync<T>(string sql, CancellationToken ct = default);
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryRowsAsync(string sql, CancellationToken ct = default);
}

/// <summary>Applies foundation schema migrations for a local database.</summary>
public interface ILocalDatabaseMigrator
{
    int CurrentSchemaVersion { get; }

    Task<LocalMigrationResult> MigrateAsync(
        ILocalDatabaseConnection connection,
        LocalContextIdentity identity,
        CancellationToken ct = default);
}

/// <summary>Manages the single active per-user/org/product local database context.</summary>
public interface ILocalContextManager
{
    LocalContextSnapshot? ActiveContext { get; }

    /// <summary>
    /// Opens (or switches to) the isolated database after online access validation.
    /// Closes any previous active context first.
    /// Rejects <see cref="Offline.PersonalLocalScope.PathIsolationMarker"/> — use
    /// <see cref="OpenPersonalAsync"/> for Personal scope.
    /// </summary>
    Task<LocalContextOpenResult> OpenAsync(
        Guid userId,
        Guid organizationId,
        string productCode,
        CancellationToken ct = default);

    /// <summary>
    /// Opens the Personal-scope local database for the user (path-isolated from org POS DBs).
    /// </summary>
    Task<LocalContextOpenResult> OpenPersonalAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>Closes the active connection and clears in-memory context. Does not delete the file.</summary>
    Task CloseAsync(CancellationToken ct = default);
}

public sealed record LocalContextIdentity(
    string ContextHash,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode);

public sealed record LocalContextSnapshot(
    LocalContextIdentity Identity,
    string DatabaseFileName,
    int SchemaVersion,
    DateTimeOffset OpenedAtUtc,
    LocalContextInitStatus Status);

public enum LocalContextInitStatus
{
    NotInitialized = 0,
    Ready = 1,
    Failed = 2,
    Closed = 3
}

public sealed record LocalMigrationResult(
    bool Succeeded,
    int SchemaVersion,
    string? ErrorCode = null);

public sealed record LocalContextOpenResult(
    bool Succeeded,
    LocalContextSnapshot? Context = null,
    string? ErrorCode = null);
