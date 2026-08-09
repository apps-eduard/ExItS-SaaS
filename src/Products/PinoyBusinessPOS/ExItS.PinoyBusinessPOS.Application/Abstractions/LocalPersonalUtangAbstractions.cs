namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public static class LocalPersonalSyncStatus
{
    public const string Synced = "Synced";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
}

public static class LocalPersonalDirection
{
    public const string Lent = "lent";
    public const string Borrowed = "borrowed";
}

public sealed record LocalPersonalContact(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? Phone,
    string? Notes,
    string SyncStatus,
    Guid? ServerId,
    DateTimeOffset UpdatedAtUtc,
    Guid? OperationId);

public sealed record LocalPersonalRelationship(
    Guid Id,
    Guid UserId,
    Guid ContactId,
    string Direction,
    decimal Outstanding,
    string Currency,
    string SyncStatus,
    Guid? ServerId,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    Guid? OperationId);

public sealed record LocalPersonalEntry(
    Guid Id,
    Guid RelationshipId,
    string EntryType,
    decimal Amount,
    string? Note,
    DateTimeOffset OccurredAtUtc,
    string SyncStatus,
    Guid? ServerId,
    Guid? OperationId,
    DateTimeOffset CreatedAtUtc);

public sealed record LocalPersonalContactUpsertCommand(
    Guid ContactId,
    Guid OperationId,
    string IdempotencyKey,
    string DisplayName,
    string? Phone,
    string? Notes);

public sealed record LocalPersonalRelationshipCreateCommand(
    Guid RelationshipId,
    Guid OperationId,
    string IdempotencyKey,
    Guid ContactId,
    string Direction,
    decimal InitialAmount,
    string Currency,
    string? Notes,
    Guid? DependsOnContactOperationId = null);

public sealed record LocalPersonalEntryRecordCommand(
    Guid EntryId,
    Guid OperationId,
    string IdempotencyKey,
    Guid RelationshipId,
    string EntryType,
    decimal Amount,
    string? Note,
    DateTimeOffset? OccurredAtUtc = null,
    Guid? DependsOnRelationshipOperationId = null);

public sealed record LocalPersonalAggregates(
    int ContactCount,
    int ActiveRelationshipCount,
    decimal TotalLentBalance,
    decimal TotalBorrowedBalance);

public static class LocalPersonalStoreErrors
{
    public const string EmailConflict = "email_conflict";
}

/// <summary>Local-first Personal Utang store with transactional outbox enqueue.</summary>
public interface ILocalPersonalUtangStore
{
    Task EnsurePersonalContextAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LocalPersonalContact>> ListContactsAsync(CancellationToken ct = default);

    Task<LocalPersonalContact?> GetContactAsync(Guid contactId, CancellationToken ct = default);

    /// <summary>
    /// Finds a contact whose Notes (email) matches the normalized email (case-insensitive).
    /// </summary>
    Task<LocalPersonalContact?> FindContactByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken ct = default);

    Task<IReadOnlyList<LocalPersonalRelationship>> ListRelationshipsAsync(
        string direction,
        CancellationToken ct = default);

    Task<LocalPersonalRelationship?> GetRelationshipAsync(Guid relationshipId, CancellationToken ct = default);

    Task<IReadOnlyList<LocalPersonalEntry>> ListEntriesAsync(
        Guid relationshipId,
        CancellationToken ct = default);

    Task<LocalPersonalAggregates> GetAggregatesAsync(CancellationToken ct = default);

    Task PersistContactAndEnqueueAsync(LocalPersonalContactUpsertCommand command, CancellationToken ct = default);

    Task PersistRelationshipAndEnqueueAsync(
        LocalPersonalRelationshipCreateCommand command,
        CancellationToken ct = default);

    Task PersistEntryAndEnqueueAsync(LocalPersonalEntryRecordCommand command, CancellationToken ct = default);

    /// <summary>Upserts a server-confirmed contact without enqueueing outbox work.</summary>
    Task UpsertServerContactAsync(LocalPersonalContact contact, CancellationToken ct = default);

    /// <summary>Upserts a server-confirmed relationship without enqueueing outbox work.</summary>
    Task UpsertServerRelationshipAsync(LocalPersonalRelationship relationship, CancellationToken ct = default);

    /// <summary>Pending/retryable personal outbox rows for the active personal context.</summary>
    Task<int> CountPendingSyncAsync(CancellationToken ct = default);

    Task MarkContactSyncedAsync(Guid contactId, Guid serverId, CancellationToken ct = default);

    Task MarkRelationshipSyncedAsync(Guid relationshipId, Guid serverId, int version, CancellationToken ct = default);

    Task MarkEntrySyncedAsync(Guid entryId, Guid serverId, CancellationToken ct = default);
}
