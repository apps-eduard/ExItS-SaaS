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
    ReconnectRequired = 6,
    /// <summary>Conflict, permanent failure, blocked-by-access, or encryption-key recovery required.</summary>
    RecoveryRequired = 7
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

    /// <summary>Marks encryption-key or other recovery-required condition (fail-closed; work retained).</summary>
    void SetRecoveryRequired(bool required);

    void Refresh();
}

/// <summary>
/// Gate for protected POS shell entry. Online validation unlocks the process session;
/// mid-session offline continues only while that continuous validated session remains active.
/// </summary>
public interface IProtectedShellAccessPolicy
{
    bool CanEnterProtectedShell { get; }

    /// <summary>True when the user is authenticated but must reconnect online to verify access.</summary>
    bool RequiresReconnectToVerifyAccess { get; }

    /// <summary>True when offline customer/credit mutations are allowed for this continuous session.</summary>
    bool AllowsOfflineMutation { get; }

    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Clears process-lifetime online validation (logout / context switch).</summary>
    void ClearProcessValidation();

    /// <summary>
    /// Re-evaluates process validation after org bind / session update while online.
    /// </summary>
    void NotifySessionAccessChanged();

    /// <summary>
    /// Marks this process as validated from a PIN-unlocked offline operate grant (cold start).
    /// Does not extend grant expiry; only restores continuous offline mutation rights.
    /// </summary>
    void NotifyOfflineUnlock(Guid userId, Guid? organizationId);
}
