namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Local encrypted projection entity states (customer/credit/repayment/due-date).</summary>
public enum LocalEntitySyncState
{
    ServerConfirmed = 0,
    PendingCreate = 1,
    PendingUpdate = 2,
    Syncing = 3,
    Conflict = 4,
    Rejected = 5,
    PendingReversal = 6,
    BlockedByAccess = 7
}

/// <summary>Decrypted customer projection for UI (never logged).</summary>
public sealed record LocalCustomerProjection(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    LocalEntitySyncState EntityState,
    Guid? PendingOperationId,
    string? ConcurrencyToken,
    string? ConflictServerJson,
    string? SafeFailureCode);

/// <summary>Decrypted credit projection for UI (never logged).</summary>
public sealed record LocalCreditProjection(
    Guid CreditEntryId,
    Guid CustomerId,
    Guid OrganizationId,
    decimal Amount,
    string Remarks,
    string Status,
    DateTimeOffset CreatedAtUtc,
    LocalEntitySyncState EntityState,
    Guid? PendingOperationId,
    Guid? DependsOnOperationId,
    string? SafeFailureCode,
    DateOnly? CurrentDueDate = null,
    DateOnly? PendingDueDate = null,
    string? PendingDueDateReason = null,
    bool PendingDueDateClear = false,
    string? ConflictServerJson = null);

/// <summary>Decrypted repayment projection for UI (never logged).</summary>
public sealed record LocalRepaymentProjection(
    Guid RepaymentId,
    Guid CustomerId,
    Guid OrganizationId,
    decimal Amount,
    string? Remarks,
    string Status,
    DateTimeOffset RecordedAtUtc,
    LocalEntitySyncState EntityState,
    Guid? PendingOperationId,
    Guid? DependsOnOperationId,
    string? SafeFailureCode,
    string? PendingReversalReason = null);

/// <summary>Confirmed vs pending outstanding projection (read-model only).</summary>
public sealed record LocalCustomerBalanceProjection(
    Guid CustomerId,
    decimal ConfirmedOutstanding,
    decimal PendingCredit,
    decimal PendingRepayment,
    decimal ProjectedOutstanding);

public sealed record LocalCustomerCreateCommand(
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes);

public sealed record LocalCustomerUpdateCommand(
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string ExpectedConcurrencyToken);

public sealed record LocalCreditCreateCommand(
    Guid CreditEntryId,
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    decimal Amount,
    string Remarks,
    Guid? DependsOnCustomerCreateOperationId);

public sealed record LocalRepaymentCreateCommand(
    Guid RepaymentId,
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    decimal Amount,
    string? Remarks,
    Guid? DependsOnCustomerCreateOperationId,
    Guid? DependsOnCreditCreateOperationId);

public sealed record LocalCreditReverseCommand(
    Guid CreditEntryId,
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    string Reason);

public sealed record LocalRepaymentReverseCommand(
    Guid RepaymentId,
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    string Reason);

public sealed record LocalCreditDueDateCommand(
    Guid CreditEntryId,
    Guid CustomerId,
    Guid OperationId,
    string IdempotencyKey,
    DateOnly? NewDueDate,
    string Reason,
    bool IsClear,
    string? ExpectedConcurrencyToken);

/// <summary>Encrypted local customer/credit/repayment read model + transactional enqueue.</summary>
public interface ILocalCustomerCreditStore
{
    Task UpsertServerCustomerAsync(LocalCustomerProjection customer, CancellationToken ct = default);

    Task UpsertServerCreditAsync(LocalCreditProjection credit, CancellationToken ct = default);

    Task UpsertServerRepaymentAsync(LocalRepaymentProjection repayment, CancellationToken ct = default);

    Task SetConfirmedOutstandingAsync(Guid customerId, decimal confirmedOutstanding, CancellationToken ct = default);

    Task PersistCustomerCreateAndEnqueueAsync(LocalCustomerCreateCommand command, CancellationToken ct = default);

    Task PersistCustomerUpdateAndEnqueueAsync(LocalCustomerUpdateCommand command, CancellationToken ct = default);

    Task PersistCreditCreateAndEnqueueAsync(LocalCreditCreateCommand command, CancellationToken ct = default);

    Task PersistRepaymentCreateAndEnqueueAsync(LocalRepaymentCreateCommand command, CancellationToken ct = default);

    Task PersistCreditReverseAndEnqueueAsync(LocalCreditReverseCommand command, CancellationToken ct = default);

    Task PersistRepaymentReverseAndEnqueueAsync(LocalRepaymentReverseCommand command, CancellationToken ct = default);

    Task PersistCreditDueDateAndEnqueueAsync(LocalCreditDueDateCommand command, CancellationToken ct = default);

    Task MarkCustomerStateAsync(
        Guid customerId,
        LocalEntitySyncState state,
        string? concurrencyToken = null,
        string? conflictServerJson = null,
        string? safeFailureCode = null,
        CancellationToken ct = default);

    Task MarkCreditStateAsync(
        Guid creditEntryId,
        LocalEntitySyncState state,
        string? safeFailureCode = null,
        CancellationToken ct = default);

    Task MarkRepaymentStateAsync(
        Guid repaymentId,
        LocalEntitySyncState state,
        string? safeFailureCode = null,
        CancellationToken ct = default);

    Task DiscardLocalCustomerUpdateAsync(Guid customerId, LocalCustomerProjection serverVersion, CancellationToken ct = default);

    /// <summary>Discard a never-server-confirmed local request; does not delete server transactions.</summary>
    Task DiscardLocalPendingRepaymentAsync(Guid repaymentId, CancellationToken ct = default);

    Task DiscardLocalPendingCreditDueDateAsync(Guid creditEntryId, CancellationToken ct = default);

    Task DiscardLocalPendingCreditReversalAsync(Guid creditEntryId, CancellationToken ct = default);

    Task DiscardLocalPendingRepaymentReversalAsync(Guid repaymentId, CancellationToken ct = default);

    /// <summary>Rebuild pending credit/repayment totals from unresolved queue-linked projections.</summary>
    Task RebuildOptimisticBalancesAsync(Guid customerId, CancellationToken ct = default);

    Task<LocalCustomerProjection?> GetCustomerAsync(Guid customerId, CancellationToken ct = default);

    Task<IReadOnlyList<LocalCustomerProjection>> ListCustomersAsync(
        string? search,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<int> CountCustomersAsync(string? search, CancellationToken ct = default);

    Task<IReadOnlyList<LocalCreditProjection>> ListCreditsAsync(Guid customerId, CancellationToken ct = default);

    Task<LocalCreditProjection?> GetCreditAsync(Guid creditEntryId, CancellationToken ct = default);

    Task<IReadOnlyList<LocalRepaymentProjection>> ListRepaymentsAsync(Guid customerId, CancellationToken ct = default);

    Task<LocalRepaymentProjection?> GetRepaymentAsync(Guid repaymentId, CancellationToken ct = default);

    Task<LocalCustomerBalanceProjection> GetBalanceAsync(Guid customerId, CancellationToken ct = default);

    Task SetDownloadCheckpointAsync(string stream, DateTimeOffset checkpointUtc, CancellationToken ct = default);

    Task<DateTimeOffset?> GetDownloadCheckpointAsync(string stream, CancellationToken ct = default);

    Task MarkDependentsBlockedAsync(Guid dependencyOperationId, string failureCode, CancellationToken ct = default);

    /// <summary>Entity-state counts only (no decrypted payloads). For diagnostics.</summary>
    Task<LocalEntityStateCounts> GetEntityStateCountsAsync(CancellationToken ct = default);
}

/// <summary>Aggregated local projection counts by sync state (counts only).</summary>
public sealed record LocalEntityStateCounts(
    IReadOnlyDictionary<LocalEntitySyncState, int> Customers,
    IReadOnlyDictionary<LocalEntitySyncState, int> Credits,
    IReadOnlyDictionary<LocalEntitySyncState, int> Repayments)
{
    public static LocalEntityStateCounts Empty { get; } = new(
        new Dictionary<LocalEntitySyncState, int>(),
        new Dictionary<LocalEntitySyncState, int>(),
        new Dictionary<LocalEntitySyncState, int>());
}

/// <summary>Coordinates online download, offline mutations, and reconnect reconciliation.</summary>
public interface ICustomerCreditOfflineSyncService
{
    /// <summary>True when offline mutations are allowed for the active validated session.</summary>
    bool CanMutateOffline { get; }

    Task DownloadIncrementalAsync(CancellationToken ct = default);

    Task ReconcileOnReconnectAsync(CancellationToken ct = default);

    Task<ApiResultLikeCustomer> CreateCustomerAsync(
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        Guid? platformBusinessCustomerId = null,
        CancellationToken ct = default);

    Task<ApiResultLikeCustomer> UpdateCustomerAsync(
        Guid customerId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        CancellationToken ct = default);

    Task<ApiResultLikeCredit> CreateCreditAsync(
        Guid customerId,
        decimal amount,
        string remarks,
        CancellationToken ct = default);

    Task<ApiResultLikeRepayment> CreateRepaymentAsync(
        Guid customerId,
        decimal amount,
        string? remarks,
        CancellationToken ct = default);

    Task<ApiResultLikeCredit> ReverseCreditAsync(
        Guid customerId,
        Guid creditEntryId,
        string reason,
        CancellationToken ct = default);

    Task<ApiResultLikeRepayment> ReverseRepaymentAsync(
        Guid repaymentId,
        string reason,
        CancellationToken ct = default);

    Task<ApiResultLikeCredit> SetCreditDueDateAsync(
        Guid creditEntryId,
        DateOnly? dueDate,
        string reason,
        CancellationToken ct = default);

    Task DiscardCustomerConflictAsync(Guid customerId, CancellationToken ct = default);

    Task ApplyServerCustomerAfterSuccessAsync(Guid customerId, LocalCustomerProjection server, CancellationToken ct = default);

    Task ApplyServerCreditAfterSuccessAsync(LocalCreditProjection server, CancellationToken ct = default);

    Task ApplyCreditRejectedAsync(Guid creditEntryId, string safeFailureCode, CancellationToken ct = default);

    Task ApplyRepaymentRejectedAsync(Guid repaymentId, string safeFailureCode, CancellationToken ct = default);

    Task RefreshCustomerFinancialsFromServerAsync(Guid customerId, CancellationToken ct = default);
}

/// <summary>Lightweight result used by offline coordinator without coupling UI to ApiClient types.</summary>
public sealed record ApiResultLikeCustomer(
    bool Succeeded,
    bool PendingLocal,
    LocalCustomerProjection? Customer,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ApiResultLikeCredit(
    bool Succeeded,
    bool PendingLocal,
    LocalCreditProjection? Credit,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ApiResultLikeRepayment(
    bool Succeeded,
    bool PendingLocal,
    LocalRepaymentProjection? Repayment,
    string? ErrorCode,
    string? ErrorMessage);
