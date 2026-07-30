using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Explicit offline outbox states. Never reduce to a single IsSynced flag.</summary>
public enum OfflineQueueState
{
    Pending = 0,
    Syncing = 1,
    Succeeded = 2,
    RetryableFailure = 3,
    PermanentFailure = 4,
    Conflict = 5,
    BlockedByAccess = 6
}

/// <summary>Classifies processor/transport outcomes for retry policy.</summary>
public enum OfflineFailureClass
{
    None = 0,
    Transient = 1,
    Permanent = 2,
    Conflict = 3,
    AccessBlocked = 4
}

/// <summary>Generic offline operation envelope (metadata only — payload remains encrypted).</summary>
public sealed record OfflineOperationEnvelope(
    Guid OperationId,
    string DeviceId,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    string OperationType,
    int PayloadVersion,
    string PayloadHash,
    string IdempotencyKey,
    DateTimeOffset CreatedUtc,
    DateTimeOffset NextAttemptUtc,
    int AttemptCount,
    OfflineQueueState QueueState,
    DateTimeOffset? LastAttemptUtc,
    string? FailureCode,
    string? FailureSummary,
    string? ServerReference,
    string? ConcurrencyToken,
    Guid? DependsOnOperationId = null,
    Guid? EntityId = null);

public sealed record EncryptedPayload(
    byte[] Ciphertext,
    byte[] Nonce,
    byte[] Tag);

public sealed record OfflineEnqueueRequest(
    Guid OperationId,
    string OperationType,
    int PayloadVersion,
    string IdempotencyKey,
    ReadOnlyMemory<byte> PlaintextPayload,
    string? ConcurrencyToken = null,
    Guid? DependsOnOperationId = null,
    Guid? EntityId = null);

public sealed record OfflineQueueCounts(
    int Pending,
    int Syncing,
    int Succeeded,
    int RetryableFailure,
    int PermanentFailure,
    int Conflict,
    int BlockedByAccess)
{
    public int UnsyncedWork => Pending + Syncing + RetryableFailure + PermanentFailure + Conflict + BlockedByAccess;

    public int PendingSyncDisplay => Pending + RetryableFailure;
}

/// <summary>Encrypts/decrypts offline operation payloads. Key lives only in SecureStorage.</summary>
public interface ILocalPayloadProtector
{
    Task EnsureKeyAsync(CancellationToken ct = default);

    Task<bool> IsKeyAvailableAsync(CancellationToken ct = default);

    Task<EncryptedPayload> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        string associatedData,
        CancellationToken ct = default);

    Task<byte[]> DecryptAsync(
        EncryptedPayload encrypted,
        string associatedData,
        CancellationToken ct = default);
}

/// <summary>Persists and claims the generic offline outbox for the active local context.</summary>
public interface IOfflineOperationQueue
{
    Task EnqueueAsync(OfflineEnqueueRequest request, CancellationToken ct = default);

    Task RecoverAbandonedSyncingAsync(CancellationToken ct = default);

    /// <summary>
    /// Moves BlockedByAccess rows back to Pending when access is restored so they can be reclaimed.
    /// Does not delete operations.
    /// </summary>
    Task ReclaimBlockedByAccessAsync(CancellationToken ct = default);

    Task<OfflineOperationEnvelope?> TryClaimNextAsync(string claimToken, CancellationToken ct = default);

    Task MarkSucceededAsync(Guid operationId, string? serverReference, CancellationToken ct = default);

    Task MarkFailureAsync(
        Guid operationId,
        OfflineFailureClass failureClass,
        string failureCode,
        string? failureSummary,
        DateTimeOffset? nextAttemptUtc,
        int attemptCount,
        CancellationToken ct = default);

    Task<OfflineQueueCounts> GetCountsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<OfflineOperationEnvelope>> ListSafeMetadataAsync(int take, CancellationToken ct = default);

    Task<bool> HasUnsyncedWorkAsync(CancellationToken ct = default);

    Task SetLastSyncedUtcAsync(DateTimeOffset utc, CancellationToken ct = default);

    Task<DateTimeOffset?> GetLastSyncedUtcAsync(CancellationToken ct = default);

    /// <summary>Loads encrypted payload for processing. Callers must never log ciphertext or plaintext.</summary>
    Task<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?> TryLoadEncryptedAsync(
        Guid operationId,
        CancellationToken ct = default);
}

/// <summary>Revalidates online access before processing queued work.</summary>
public interface IOfflineAccessRevalidator
{
    Task<OfflineAccessRevalidationResult> RevalidateAsync(CancellationToken ct = default);

    /// <summary>Revalidates operation-specific capability for a claimed queue item before dispatch.</summary>
    Task<OfflineAccessRevalidationResult> RevalidateOperationAsync(
        string operationType,
        CancellationToken ct = default);
}

public sealed record OfflineAccessRevalidationResult(
    bool Allowed,
    string? ReasonCode);

/// <summary>Classifies transport/HTTP outcomes into retry classes.</summary>
public interface IOfflineRetryClassifier
{
    OfflineFailureClass Classify(ApiCallStatus status, int? httpStatusCode = null);

    DateTimeOffset ComputeNextAttemptUtc(int attemptCount, DateTimeOffset nowUtc);

    int MaxAttempts { get; }
}

/// <summary>Drives FIFO processing for the active context.</summary>
public interface IOfflineQueueProcessor
{
    Task<OfflineProcessBatchResult> ProcessAvailableAsync(CancellationToken ct = default);
}

public sealed record OfflineProcessBatchResult(
    int Processed,
    int Succeeded,
    int Failed,
    string? SafeStatusKey);

/// <summary>Dispatches a decrypted operation to a typed handler (Dev probe or future business handlers).</summary>
public interface IOfflineOperationDispatcher
{
    bool CanHandle(string operationType);

    Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default);
}

public sealed record OfflineDispatchResult(
    bool Succeeded,
    OfflineFailureClass FailureClass,
    string? FailureCode,
    string? FailureSummary,
    string? ServerReference,
    int? HttpStatusCode = null);

/// <summary>Server-side idempotency for POS mutations (Application contract).</summary>
public interface IPosIdempotencyService
{
    Task<PosIdempotencyOutcome> ExecuteAsync(
        PosIdempotencyRequest request,
        Func<CancellationToken, Task<PosIdempotencyExecutionResult>> execute,
        CancellationToken ct = default);
}

public sealed record PosIdempotencyRequest(
    Guid OrganizationId,
    string ProductCode,
    string OperationType,
    string IdempotencyKey,
    string PayloadHash,
    Guid? OperationId = null);

public sealed record PosIdempotencyExecutionResult(
    string OutcomeCode,
    string? OutcomeBodyJson,
    string? ServerReference);

public sealed record PosIdempotencyOutcome(
    bool IsReplay,
    bool IsConflict,
    string OutcomeCode,
    string? OutcomeBodyJson,
    string? ServerReference);
