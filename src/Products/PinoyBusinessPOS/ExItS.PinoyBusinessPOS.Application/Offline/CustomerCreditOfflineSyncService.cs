using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Online download + offline enqueue coordinator for customers and remarks-based credit.
/// Offline mutations require a continuous process-validated session (no entitlement grace).
/// </summary>
public sealed class CustomerCreditOfflineSyncService(
    IPosCustomerClient api,
    ILocalCustomerCreditStore store,
    ILocalContextManager contextManager,
    IProtectedShellAccessPolicy accessPolicy,
    IConnectivityService connectivity,
    ICurrentUserContext currentUser,
    IOfflineQueueProcessor queueProcessor,
    IPosSyncStatusService syncStatus,
    TimeProvider? timeProvider = null) : ICustomerCreditOfflineSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public bool CanMutateOffline =>
        accessPolicy.AllowsOfflineMutation
        && contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
        && currentUser.Session?.OrganizationId is not null;

    public async Task DownloadIncrementalAsync(CancellationToken ct = default)
    {
        if (!await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        if (contextManager.ActiveContext?.Status != LocalContextInitStatus.Ready)
        {
            return;
        }

        await DownloadCustomersAsync(ct).ConfigureAwait(false);
        await DownloadCreditsAsync(ct).ConfigureAwait(false);
        syncStatus.Refresh();
    }

    public async Task ReconcileOnReconnectAsync(CancellationToken ct = default)
    {
        var access = accessPolicy;
        await access.InitializeAsync(ct).ConfigureAwait(false);
        if (!access.CanEnterProtectedShell)
        {
            syncStatus.SetReconnectRequired(true);
            syncStatus.Refresh();
            return;
        }

        syncStatus.SetReconnectRequired(false);
        await DownloadIncrementalAsync(ct).ConfigureAwait(false);
        await queueProcessor.ProcessAvailableAsync(ct).ConfigureAwait(false);
        await DownloadIncrementalAsync(ct).ConfigureAwait(false);
        syncStatus.Refresh();
    }

    public async Task<ApiResultLikeCustomer> CreateCustomerAsync(
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        CancellationToken ct = default)
    {
        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            var customerId = Guid.NewGuid();
            var request = new CreatePosCustomerRequest(displayName, mobileNumber, address, notes, customerId);
            var payload = JsonSerializer.Serialize(new
            {
                customerId,
                displayName,
                mobileNumber,
                address,
                notes
            }, JsonOptions);
            var hash = Sha256Hex(payload);
            var opId = Guid.NewGuid();
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                hash,
                opId,
                OfflineOperationTypes.CustomerCreate);

            var result = await api.CreateAsync(request, headers, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapCustomer(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerCustomerAsync(projection, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeCustomer(true, false, projection, null, null);
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline)
            {
                return await EnqueueCustomerCreateAsync(displayName, mobileNumber, address, notes, ct)
                    .ConfigureAwait(false);
            }

            return new ApiResultLikeCustomer(
                false,
                false,
                null,
                result.Error?.ErrorCode,
                result.Error?.Detail ?? result.Status.ToString());
        }

        if (!CanMutateOffline)
        {
            return new ApiResultLikeCustomer(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        return await EnqueueCustomerCreateAsync(displayName, mobileNumber, address, notes, ct).ConfigureAwait(false);
    }

    public async Task<ApiResultLikeCustomer> UpdateCustomerAsync(
        Guid customerId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        CancellationToken ct = default)
    {
        var local = contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
            ? await store.GetCustomerAsync(customerId, ct).ConfigureAwait(false)
            : null;

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            DateTimeOffset? expected = null;
            if (local?.ConcurrencyToken is not null
                && DateTimeOffset.TryParse(local.ConcurrencyToken, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var token))
            {
                expected = token;
            }
            else if (local is not null)
            {
                expected = local.UpdatedAtUtc;
            }

            var request = new UpdatePosCustomerRequest(displayName, mobileNumber, address, notes, expected);
            var payload = JsonSerializer.Serialize(new
            {
                customerId,
                displayName,
                mobileNumber,
                address,
                notes,
                expectedUpdatedAtUtc = expected?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
            }, JsonOptions);
            var opId = Guid.NewGuid();
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                Sha256Hex(payload),
                opId,
                OfflineOperationTypes.CustomerUpdate);

            var result = await api.UpdateAsync(customerId, request, headers, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapCustomer(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerCustomerAsync(projection, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeCustomer(true, false, projection, null, null);
            }

            if (result.Status == ApiCallStatus.Conflict)
            {
                if (local is not null && contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    var serverJson = result.Data is not null
                        ? JsonSerializer.Serialize(result.Data, JsonOptions)
                        : result.Error?.Detail;
                    await store.MarkCustomerStateAsync(
                            customerId,
                            LocalEntitySyncState.Conflict,
                            conflictServerJson: serverJson,
                            safeFailureCode: ApplicationErrorCodes.CustomerConcurrencyConflict,
                            ct: ct)
                        .ConfigureAwait(false);
                }

                return new ApiResultLikeCustomer(
                    false,
                    false,
                    local,
                    ApplicationErrorCodes.CustomerConcurrencyConflict,
                    result.Error?.Detail ?? "Customer was updated elsewhere.");
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline && local is not null)
            {
                return await EnqueueCustomerUpdateAsync(local, displayName, mobileNumber, address, notes, ct)
                    .ConfigureAwait(false);
            }

            return new ApiResultLikeCustomer(false, false, local, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline || local is null)
        {
            return new ApiResultLikeCustomer(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        return await EnqueueCustomerUpdateAsync(local, displayName, mobileNumber, address, notes, ct).ConfigureAwait(false);
    }

    public async Task<ApiResultLikeCredit> CreateCreditAsync(
        Guid customerId,
        decimal amount,
        string remarks,
        CancellationToken ct = default)
    {
        var customer = contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
            ? await store.GetCustomerAsync(customerId, ct).ConfigureAwait(false)
            : null;

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online && (customer is null || customer.EntityState == LocalEntitySyncState.ServerConfirmed))
        {
            var entryId = Guid.NewGuid();
            var request = new CreatePosCreditEntryRequest(amount, remarks, entryId);
            var payload = JsonSerializer.Serialize(new
            {
                creditEntryId = entryId,
                customerId,
                amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
                remarks
            }, JsonOptions);
            var opId = Guid.NewGuid();
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                Sha256Hex(payload),
                opId,
                OfflineOperationTypes.CreditCreate);

            var result = await api.CreateCreditEntryAsync(customerId, request, headers, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapCredit(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerCreditAsync(projection, ct).ConfigureAwait(false);
                    var summary = await api.GetCreditSummaryAsync(customerId, ct).ConfigureAwait(false);
                    if (summary.IsSuccess && summary.Data is not null)
                    {
                        await store.SetConfirmedOutstandingAsync(customerId, summary.Data.OutstandingAmount, ct)
                            .ConfigureAwait(false);
                    }
                }

                return new ApiResultLikeCredit(true, false, projection, null, null);
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline)
            {
                return await EnqueueCreditCreateAsync(customerId, amount, remarks, customer, ct).ConfigureAwait(false);
            }

            return new ApiResultLikeCredit(false, false, null, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline)
        {
            return new ApiResultLikeCredit(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        if (customer is null)
        {
            return new ApiResultLikeCredit(false, false, null, ApplicationErrorCodes.CustomerNotFound, "Customer was not found locally.");
        }

        if (!string.Equals(customer.Status, nameof(CustomerStatus.Active), StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResultLikeCredit(false, false, null, "customer_not_active", "Credit can only be recorded for an active customer.");
        }

        return await EnqueueCreditCreateAsync(customerId, amount, remarks, customer, ct).ConfigureAwait(false);
    }

    public async Task DiscardCustomerConflictAsync(Guid customerId, CancellationToken ct = default)
    {
        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (!online)
        {
            return;
        }

        var server = await api.GetAsync(customerId, ct).ConfigureAwait(false);
        if (!server.IsSuccess || server.Data is null)
        {
            return;
        }

        var projection = MapCustomer(server.Data, LocalEntitySyncState.ServerConfirmed);
        await store.DiscardLocalCustomerUpdateAsync(customerId, projection, ct).ConfigureAwait(false);
    }

    public Task ApplyServerCustomerAfterSuccessAsync(
        Guid customerId,
        LocalCustomerProjection server,
        CancellationToken ct = default) =>
        store.UpsertServerCustomerAsync(server with { CustomerId = customerId, EntityState = LocalEntitySyncState.ServerConfirmed }, ct);

    public Task ApplyServerCreditAfterSuccessAsync(LocalCreditProjection server, CancellationToken ct = default) =>
        store.UpsertServerCreditAsync(server with { EntityState = LocalEntitySyncState.ServerConfirmed }, ct);

    public Task ApplyCreditRejectedAsync(Guid creditEntryId, string safeFailureCode, CancellationToken ct = default) =>
        store.MarkCreditStateAsync(creditEntryId, LocalEntitySyncState.Rejected, safeFailureCode, ct);

    private async Task<ApiResultLikeCustomer> EnqueueCustomerCreateAsync(
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        CancellationToken ct)
    {
        var customerId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var command = new LocalCustomerCreateCommand(
            customerId,
            operationId,
            operationId.ToString("N"),
            displayName,
            mobileNumber,
            address,
            notes);

        await store.PersistCustomerCreateAndEnqueueAsync(command, ct).ConfigureAwait(false);
        syncStatus.Refresh();
        var created = await store.GetCustomerAsync(customerId, ct).ConfigureAwait(false);
        return new ApiResultLikeCustomer(true, true, created, null, null);
    }

    private async Task<ApiResultLikeCustomer> EnqueueCustomerUpdateAsync(
        LocalCustomerProjection local,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        CancellationToken ct)
    {
        var operationId = Guid.NewGuid();
        var token = local.ConcurrencyToken
            ?? local.UpdatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        await store.PersistCustomerUpdateAndEnqueueAsync(
                new LocalCustomerUpdateCommand(
                    local.CustomerId,
                    operationId,
                    operationId.ToString("N"),
                    displayName,
                    mobileNumber,
                    address,
                    notes,
                    token),
                ct)
            .ConfigureAwait(false);
        syncStatus.Refresh();
        var updated = await store.GetCustomerAsync(local.CustomerId, ct).ConfigureAwait(false);
        return new ApiResultLikeCustomer(true, true, updated, null, null);
    }

    private async Task<ApiResultLikeCredit> EnqueueCreditCreateAsync(
        Guid customerId,
        decimal amount,
        string remarks,
        LocalCustomerProjection? customer,
        CancellationToken ct)
    {
        Guid? dependsOn = null;
        if (customer?.EntityState is LocalEntitySyncState.PendingCreate or LocalEntitySyncState.Syncing
            && customer.PendingOperationId is Guid pendingOp)
        {
            dependsOn = pendingOp;
        }

        var creditId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await store.PersistCreditCreateAndEnqueueAsync(
                new LocalCreditCreateCommand(
                    creditId,
                    customerId,
                    operationId,
                    operationId.ToString("N"),
                    amount,
                    remarks,
                    dependsOn),
                ct)
            .ConfigureAwait(false);
        syncStatus.Refresh();
        var credits = await store.ListCreditsAsync(customerId, ct).ConfigureAwait(false);
        var credit = credits.FirstOrDefault(c => c.CreditEntryId == creditId);
        return new ApiResultLikeCredit(true, true, credit, null, null);
    }

    private async Task DownloadCustomersAsync(CancellationToken ct)
    {
        var since = await store.GetDownloadCheckpointAsync("customers", ct).ConfigureAwait(false);
        var page = 1;
        DateTimeOffset? maxUtc = since;
        while (true)
        {
            var result = await api.SyncCustomersAsync(since, page, 100, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null)
            {
                break;
            }

            foreach (var item in result.Data.Items)
            {
                await store.UpsertServerCustomerAsync(MapCustomer(item, LocalEntitySyncState.ServerConfirmed), ct)
                    .ConfigureAwait(false);
                if (maxUtc is null || item.UpdatedAtUtc > maxUtc)
                {
                    maxUtc = item.UpdatedAtUtc;
                }
            }

            if (result.Data.Items.Count < result.Data.PageSize || result.Data.Items.Count == 0)
            {
                break;
            }

            page++;
        }

        if (maxUtc is not null)
        {
            await store.SetDownloadCheckpointAsync("customers", maxUtc.Value, ct).ConfigureAwait(false);
        }
    }

    private async Task DownloadCreditsAsync(CancellationToken ct)
    {
        var since = await store.GetDownloadCheckpointAsync("credit-entries", ct).ConfigureAwait(false);
        var page = 1;
        DateTimeOffset? maxUtc = since;
        while (true)
        {
            var result = await api.SyncCreditEntriesAsync(since, page, 100, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null)
            {
                break;
            }

            foreach (var item in result.Data.Items)
            {
                await store.UpsertServerCreditAsync(MapCredit(item, LocalEntitySyncState.ServerConfirmed), ct)
                    .ConfigureAwait(false);
                var stamp = item.ReversedAtUtc ?? item.CreatedAtUtc;
                if (maxUtc is null || stamp > maxUtc)
                {
                    maxUtc = stamp;
                }

                var summary = await api.GetCreditSummaryAsync(item.CustomerId, ct).ConfigureAwait(false);
                if (summary.IsSuccess && summary.Data is not null)
                {
                    await store.SetConfirmedOutstandingAsync(item.CustomerId, summary.Data.OutstandingAmount, ct)
                        .ConfigureAwait(false);
                }
            }

            if (result.Data.Items.Count < result.Data.PageSize || result.Data.Items.Count == 0)
            {
                break;
            }

            page++;
        }

        if (maxUtc is not null)
        {
            await store.SetDownloadCheckpointAsync("credit-entries", maxUtc.Value, ct).ConfigureAwait(false);
        }
    }

    private static bool ShouldFallbackOffline(ApiCallStatus status) =>
        status is ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Unavailable;

    private static LocalCustomerProjection MapCustomer(PosCustomerDetailDto dto, LocalEntitySyncState state) =>
        new(
            dto.CustomerId,
            dto.OrganizationId,
            dto.DisplayName,
            dto.MobileNumber,
            dto.Address,
            dto.Notes,
            dto.Status,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            state,
            null,
            dto.UpdatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            null,
            null);

    private static LocalCreditProjection MapCredit(PosCreditEntryDto dto, LocalEntitySyncState state) =>
        new(
            dto.CreditEntryId,
            dto.CustomerId,
            dto.OrganizationId,
            dto.Amount,
            dto.Remarks,
            dto.Status,
            dto.CreatedAtUtc,
            state,
            null,
            null,
            null);

    private static string Sha256Hex(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
