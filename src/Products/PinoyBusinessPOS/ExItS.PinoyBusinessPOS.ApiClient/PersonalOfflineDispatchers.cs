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
    ILocalPersonalUtangStore? localStore = null) : IOfflineOperationDispatcher
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

        var result = await platform
            .CreatePersonalContactAsync(
                new CreatePersonalContactRequest(payload.DisplayName, payload.Phone, null),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore
                    .MarkContactSyncedAsync(payload.ContactId, result.Data.Id, ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return MapFailure(result.Status, result.Error?.ErrorCode);
    }

    private static OfflineDispatchResult MapFailure(ApiCallStatus status, string? code) =>
        status switch
        {
            ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Cancelled =>
                new OfflineDispatchResult(false, OfflineFailureClass.Transient, code ?? "transient", null, null),
            _ => new OfflineDispatchResult(false, OfflineFailureClass.Permanent, code ?? "dispatch_failed", null, null)
        };
}

/// <summary>Dispatches personal.relationship.create to Platform Personal APIs only.</summary>
public sealed class PersonalRelationshipCreateOfflineDispatcher(
    IPlatformAccessClient platform,
    ILocalPersonalUtangStore? localStore = null) : IOfflineOperationDispatcher
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

        var isLent = string.Equals(payload.Direction, LocalPersonalDirection.Lent, StringComparison.OrdinalIgnoreCase);
        var request = new CreatePersonalDebtRelationshipRequest(
            CreditorUserIdentityId: isLent ? me.Data.UserIdentityId : null,
            CreditorContactId: isLent ? null : payload.ContactId,
            DebtorUserIdentityId: isLent ? null : me.Data.UserIdentityId,
            DebtorContactId: isLent ? payload.ContactId : null,
            CurrencyCode: payload.Currency,
            DueDateUtc: null,
            InitialLoanAmount: payload.InitialAmount > 0 ? payload.InitialAmount : null,
            InitialLoanNotes: payload.Notes);

        var result = await platform.CreatePersonalDebtRelationshipAsync(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore
                    .MarkRelationshipSyncedAsync(
                        payload.RelationshipId,
                        result.Data.Id,
                        result.Data.Version,
                        ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return statusFailure(result.Status, result.Error?.ErrorCode);
    }

    private static OfflineDispatchResult statusFailure(ApiCallStatus status, string? code) =>
        status is ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Cancelled
            ? new OfflineDispatchResult(false, OfflineFailureClass.Transient, code ?? "transient", null, null)
            : new OfflineDispatchResult(false, OfflineFailureClass.Permanent, code ?? "dispatch_failed", null, null);
}

/// <summary>Dispatches personal.entry.record to Platform Personal APIs only.</summary>
public sealed class PersonalEntryRecordOfflineDispatcher(
    IPlatformAccessClient platform,
    ILocalPersonalUtangStore? localStore = null) : IOfflineOperationDispatcher
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
        var relationshipId = payload.RelationshipId;
        if (localStore is not null)
        {
            var local = await localStore.GetRelationshipAsync(payload.RelationshipId, ct).ConfigureAwait(false);
            if (local?.ServerId is Guid serverRel)
            {
                relationshipId = serverRel;
            }
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
            if (localStore is not null)
            {
                await localStore
                    .MarkEntrySyncedAsync(payload.EntryId, result.Data.Id, ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true, OfflineFailureClass.None, null, null, result.Data.Id.ToString("D"));
        }

        return result.Status is ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Cancelled
            ? new OfflineDispatchResult(false, OfflineFailureClass.Transient, result.Error?.ErrorCode ?? "transient", null, null)
            : new OfflineDispatchResult(false, OfflineFailureClass.Permanent, result.Error?.ErrorCode ?? "dispatch_failed", null, null);
    }
}
