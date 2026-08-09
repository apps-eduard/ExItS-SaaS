using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.ApiClient;

internal static class PersonalOfflinePayloads
{
    internal sealed record ContactUpsert(
        OfflineGrantScopeKind ScopeKind,
        Guid ContactId,
        string DisplayName,
        string? Phone,
        string? Notes);

    internal sealed record RelationshipCreate(
        OfflineGrantScopeKind ScopeKind,
        Guid RelationshipId,
        Guid ContactId,
        string Direction,
        decimal InitialAmount,
        string Currency,
        string? Notes);

    internal sealed record EntryRecord(
        OfflineGrantScopeKind ScopeKind,
        Guid EntryId,
        Guid RelationshipId,
        string EntryType,
        decimal Amount,
        string? Note,
        DateTimeOffset OccurredAtUtc);
}

/// <summary>Dispatches personal.contact.upsert to Platform Personal APIs only.</summary>
public sealed class PersonalContactUpsertOfflineDispatcher(
    IPlatformAccessClient platform,
    ILocalPersonalUtangStore localStore) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.PersonalContactUpsert, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        if (envelope.OrganizationId != PersonalLocalScope.PathIsolationMarker)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        PersonalOfflinePayloads.ContactUpsert payload;
        try
        {
            payload = JsonSerializer.Deserialize<PersonalOfflinePayloads.ContactUpsert>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (payload.ScopeKind != OfflineGrantScopeKind.Personal)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        // Create form stores email in Notes until a dedicated local email column exists.
        var result = await platform
            .CreatePersonalContactAsync(
                new CreatePersonalContactRequest(payload.DisplayName, payload.Phone, payload.Notes),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            await localStore
                .MarkContactSyncedAsync(payload.ContactId, result.Data.Id, ct)
                .ConfigureAwait(false);

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return MapFailure(result.Status, result.Error?.ErrorCode);
    }

    private static OfflineDispatchResult MapFailure(ApiCallStatus status, string? code) =>
        status switch
        {
            ApiCallStatus.Offline
                or ApiCallStatus.Timeout
                or ApiCallStatus.Cancelled
                or ApiCallStatus.Unavailable
                or ApiCallStatus.RateLimited
                or ApiCallStatus.Failed =>
                new OfflineDispatchResult(false, OfflineFailureClass.Transient, code ?? "transient", null, null),
            ApiCallStatus.Unauthorized or ApiCallStatus.Forbidden =>
                new OfflineDispatchResult(false, OfflineFailureClass.AccessBlocked, code ?? "access_blocked", null, null),
            ApiCallStatus.Conflict =>
                new OfflineDispatchResult(false, OfflineFailureClass.Conflict, code ?? "conflict", null, null),
            _ => new OfflineDispatchResult(false, OfflineFailureClass.Permanent, code ?? "dispatch_failed", null, null)
        };
}

/// <summary>Dispatches personal.relationship.create to Platform Personal APIs only.</summary>
public sealed class PersonalRelationshipCreateOfflineDispatcher(
    IPlatformAccessClient platform,
    ILocalPersonalUtangStore localStore) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.PersonalRelationshipCreate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        if (envelope.OrganizationId != PersonalLocalScope.PathIsolationMarker)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        PersonalOfflinePayloads.RelationshipCreate payload;
        try
        {
            payload = JsonSerializer.Deserialize<PersonalOfflinePayloads.RelationshipCreate>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (payload.ScopeKind != OfflineGrantScopeKind.Personal)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        var me = await platform.GetPersonalProfileAsync(ct).ConfigureAwait(false);
        if (!me.IsSuccess || me.Data is null)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Transient, "profile_unavailable", null, null);
        }

        // Outbox payloads keep the local contact PK; Platform requires the server contact id.
        var serverContactId = await ResolveServerContactIdAsync(localStore, payload.ContactId, ct)
            .ConfigureAwait(false);
        if (serverContactId is null)
        {
            // Contact create has not finished / marked synced yet — retry after dependency catches up.
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Transient, "contact_pending_sync", null, null);
        }

        var isLent = string.Equals(payload.Direction, LocalPersonalDirection.Lent, StringComparison.OrdinalIgnoreCase);
        var request = new CreatePersonalDebtRelationshipRequest(
            CreditorUserIdentityId: isLent ? me.Data.UserIdentityId : null,
            CreditorContactId: isLent ? null : serverContactId,
            DebtorUserIdentityId: isLent ? null : me.Data.UserIdentityId,
            DebtorContactId: isLent ? serverContactId : null,
            CurrencyCode: payload.Currency,
            DueDateUtc: null,
            InitialLoanAmount: payload.InitialAmount > 0 ? payload.InitialAmount : null,
            InitialLoanNotes: payload.Notes);

        var result = await platform.CreatePersonalDebtRelationshipAsync(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            await localStore
                .MarkRelationshipSyncedAsync(
                    payload.RelationshipId,
                    result.Data.Id,
                    result.Data.Version,
                    ct)
                .ConfigureAwait(false);

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return statusFailure(result.Status, result.Error?.ErrorCode);
    }

    public static async Task<Guid?> ResolveServerContactIdAsync(
        ILocalPersonalUtangStore localStore,
        Guid localOrServerContactId,
        CancellationToken ct)
    {
        var contact = await localStore.GetContactAsync(localOrServerContactId, ct).ConfigureAwait(false);
        if (contact is null)
        {
            return null;
        }

        if (contact.ServerId is Guid serverId)
        {
            return serverId;
        }

        // Hydrated contacts often use id == server id with ServerId also set; if only Id is present
        // and already synced, Id is the Platform contact id.
        if (string.Equals(contact.SyncStatus, LocalPersonalSyncStatus.Synced, StringComparison.OrdinalIgnoreCase))
        {
            return contact.Id;
        }

        return null;
    }

    private static OfflineDispatchResult statusFailure(ApiCallStatus status, string? code) =>
        status switch
        {
            ApiCallStatus.Offline
                or ApiCallStatus.Timeout
                or ApiCallStatus.Cancelled
                or ApiCallStatus.Unavailable
                or ApiCallStatus.RateLimited
                or ApiCallStatus.Failed =>
                new OfflineDispatchResult(false, OfflineFailureClass.Transient, code ?? "transient", null, null),
            ApiCallStatus.Unauthorized or ApiCallStatus.Forbidden =>
                new OfflineDispatchResult(false, OfflineFailureClass.AccessBlocked, code ?? "access_blocked", null, null),
            ApiCallStatus.Conflict =>
                new OfflineDispatchResult(false, OfflineFailureClass.Conflict, code ?? "conflict", null, null),
            _ => new OfflineDispatchResult(false, OfflineFailureClass.Permanent, code ?? "dispatch_failed", null, null)
        };
}

/// <summary>Dispatches personal.entry.record to Platform Personal APIs only.</summary>
public sealed class PersonalEntryRecordOfflineDispatcher(
    IPlatformAccessClient platform,
    ILocalPersonalUtangStore localStore) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.PersonalEntryRecord, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        if (envelope.OrganizationId != PersonalLocalScope.PathIsolationMarker)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        PersonalOfflinePayloads.EntryRecord payload;
        try
        {
            payload = JsonSerializer.Deserialize<PersonalOfflinePayloads.EntryRecord>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (payload.ScopeKind != OfflineGrantScopeKind.Personal)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "personal_scope_required", null, null);
        }

        // Prefer server relationship id when local row was already synced.
        var local = await localStore.GetRelationshipAsync(payload.RelationshipId, ct).ConfigureAwait(false);
        if (local is null)
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Permanent, "relationship_missing", null, null);
        }

        Guid relationshipId;
        if (local.ServerId is Guid serverRel)
        {
            relationshipId = serverRel;
        }
        else if (string.Equals(local.SyncStatus, LocalPersonalSyncStatus.Synced, StringComparison.OrdinalIgnoreCase))
        {
            relationshipId = local.Id;
        }
        else
        {
            return new OfflineDispatchResult(
                false, OfflineFailureClass.Transient, "relationship_pending_sync", null, null);
        }

        var result = await platform
            .RecordPersonalUtangEntryAsync(
                relationshipId,
                new RecordPersonalUtangEntryRequest(
                    payload.EntryType,
                    payload.Amount,
                    AdjustmentDelta: null,
                    ExpectedVersion: null,
                    Notes: payload.Note,
                    DueDateUtc: null),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            await localStore
                .MarkEntrySyncedAsync(payload.EntryId, result.Data.Id, ct)
                .ConfigureAwait(false);

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return MapEntryFailure(result.Status, result.Error?.ErrorCode);
    }

    private static OfflineDispatchResult MapEntryFailure(ApiCallStatus status, string? code) =>
        status switch
        {
            ApiCallStatus.Offline
                or ApiCallStatus.Timeout
                or ApiCallStatus.Cancelled
                or ApiCallStatus.Unavailable
                or ApiCallStatus.RateLimited
                or ApiCallStatus.Failed =>
                new OfflineDispatchResult(false, OfflineFailureClass.Transient, code ?? "transient", null, null),
            ApiCallStatus.Unauthorized or ApiCallStatus.Forbidden =>
                new OfflineDispatchResult(false, OfflineFailureClass.AccessBlocked, code ?? "access_blocked", null, null),
            ApiCallStatus.Conflict =>
                new OfflineDispatchResult(false, OfflineFailureClass.Conflict, code ?? "conflict", null, null),
            _ => new OfflineDispatchResult(false, OfflineFailureClass.Permanent, code ?? "dispatch_failed", null, null)
        };
}
