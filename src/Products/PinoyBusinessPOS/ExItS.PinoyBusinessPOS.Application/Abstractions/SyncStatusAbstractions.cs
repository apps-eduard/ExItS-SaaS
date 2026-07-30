namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Shared sync-status contract for the permanent POS shell indicator.
/// Queue-driven states exist in the model for P7-WP02+ but must not be fabricated in P7-WP01.
/// </summary>
public enum PosSyncStatusKind
{
    Online = 0,
    Offline = 1,
    PendingSync = 2,
    Syncing = 3,
    SyncFailed = 4,
    LastSynced = 5,
    ReconnectRequired = 6
}

/// <summary>Immutable view of the shell sync/connectivity status.</summary>
public sealed record PosSyncStatusSnapshot(
    PosSyncStatusKind Kind,
    int? PendingCount = null,
    DateTimeOffset? LastSyncedAtUtc = null,
    string? SafeDetailKey = null);

/// <summary>
/// Resolves truthful sync-status for the shell. P7-WP01 exposes Online, Offline, and
/// ReconnectRequired only; pending/syncing/failed/last-synced remain deferred.
/// </summary>
public interface IPosSyncStatusService
{
    PosSyncStatusSnapshot Current { get; }

    event Func<Task>? Changed;

    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Marks that protected access requires online revalidation (fail-closed).</summary>
    void SetReconnectRequired(bool required);

    void Refresh();
}

/// <summary>
/// Gate for protected POS shell entry. Requires online connectivity plus validated access.
/// No offline authorization window in P7-WP01.
/// </summary>
public interface IProtectedShellAccessPolicy
{
    bool CanEnterProtectedShell { get; }

    /// <summary>True when the user is authenticated but must reconnect online to verify access.</summary>
    bool RequiresReconnectToVerifyAccess { get; }

    Task InitializeAsync(CancellationToken ct = default);
}
