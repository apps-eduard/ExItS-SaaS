using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Payments;
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
    private readonly SemaphoreSlim _downloadGate = new(1, 1);

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

        if (!await _downloadGate.WaitAsync(0, ct).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await DownloadCustomersAsync(ct).ConfigureAwait(false);
            await DownloadCreditsAsync(ct).ConfigureAwait(false);
            await DownloadRepaymentsAsync(ct).ConfigureAwait(false);
            syncStatus.Refresh();
        }
        finally
        {
            _downloadGate.Release();
        }
    }

    public async Task ReconcileOnReconnectAsync(CancellationToken ct = default)
    {
        var access = accessPolicy;
        await access.InitializeAsync(ct).ConfigureAwait(false);

        // Reconnect chip = need network to verify operate access — not "no POS role yet".
        // Invited staff / owners on Org essentials are online with membership but without
        // HasPosAccess; that must not paint the header as Reconnect.
        if (access.RequiresReconnectToVerifyAccess)
        {
            syncStatus.SetReconnectRequired(true);
            syncStatus.Refresh();
            return;
        }

        if (!access.CanEnterProtectedShell)
        {
            syncStatus.SetReconnectRequired(false);
            syncStatus.Refresh();
            return;
        }

        syncStatus.SetReconnectRequired(false);
        await DownloadIncrementalAsync(ct).ConfigureAwait(false);
        if (currentUser.Session?.OrganizationId is Guid
            && contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
        {
            // Rebuild optimistic projections for known local customers before queue processing.
            var customers = await store.ListCustomersAsync(null, 0, 500, ct).ConfigureAwait(false);
            foreach (var customer in customers)
            {
                await store.RebuildOptimisticBalancesAsync(customer.CustomerId, ct).ConfigureAwait(false);
            }
        }

        await queueProcessor.ProcessAvailableAsync(ct).ConfigureAwait(false);
        await DownloadIncrementalAsync(ct).ConfigureAwait(false);
        syncStatus.Refresh();
    }

    public async Task<ApiResultLikeCustomer> CreateCustomerAsync(
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        Guid? platformBusinessCustomerId = null,
        CancellationToken ct = default)
    {
        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            var customerId = Guid.NewGuid();
            var request = new CreatePosCustomerRequest(
                displayName,
                mobileNumber,
                address,
                notes,
                customerId,
                platformBusinessCustomerId);
            var payload = JsonSerializer.Serialize(new
            {
                customerId,
                displayName,
                mobileNumber,
                address,
                notes,
                platformBusinessCustomerId
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

            // Platform-linked creates must not silently fall back to local-only without the correlation id.
            if (platformBusinessCustomerId is null
                && ShouldFallbackOffline(result.Status)
                && CanMutateOffline)
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

        if (platformBusinessCustomerId is not null)
        {
            return new ApiResultLikeCustomer(
                false,
                false,
                null,
                "offline_mutations_unavailable",
                "Personal link requires an online connection.");
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
                    // Never persist Error.Detail into conflict_server_json (may contain PHI/PII).
                    string? serverJson = result.Data is not null
                        ? JsonSerializer.Serialize(new
                        {
                            status = result.Data.Status,
                            updatedAtUtc = result.Data.UpdatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                        }, JsonOptions)
                        : null;
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

                    await store.RebuildOptimisticBalancesAsync(customerId, ct).ConfigureAwait(false);
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

    public Task ApplyRepaymentRejectedAsync(Guid repaymentId, string safeFailureCode, CancellationToken ct = default) =>
        store.MarkRepaymentStateAsync(repaymentId, LocalEntitySyncState.Rejected, safeFailureCode, ct);

    public async Task<ApiResultLikeRepayment> CreateRepaymentAsync(
        Guid customerId,
        decimal amount,
        string? remarks,
        CancellationToken ct = default)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2, MidpointRounding.AwayFromZero))
        {
            return new ApiResultLikeRepayment(false, false, null, "invalid_amount", "Amount must be positive with at most two decimal places.");
        }

        var customer = contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
            ? await store.GetCustomerAsync(customerId, ct).ConfigureAwait(false)
            : null;

        if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
        {
            var balance = await store.GetBalanceAsync(customerId, ct).ConfigureAwait(false);
            if (balance.ConfirmedOutstanding <= 0 && balance.ProjectedOutstanding <= 0)
            {
                return new ApiResultLikeRepayment(false, false, null, "zero_outstanding", "No outstanding balance to repay.");
            }

            if (amount > balance.ProjectedOutstanding)
            {
                return new ApiResultLikeRepayment(false, false, null, "local_overpayment", "Amount exceeds projected outstanding.");
            }
        }

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online && (customer is null || customer.EntityState == LocalEntitySyncState.ServerConfirmed))
        {
            var repaymentId = Guid.NewGuid();
            var request = new CreatePosRepaymentRequest(amount, remarks, repaymentId);
            var payload = JsonSerializer.Serialize(new
            {
                repaymentId,
                customerId,
                amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
                remarks
            }, JsonOptions);
            var opId = Guid.NewGuid();
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                Sha256Hex(payload),
                opId,
                OfflineOperationTypes.RepaymentCreate);

            var result = await api.CreateRepaymentAsync(customerId, request, headers, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapRepayment(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerRepaymentAsync(projection, ct).ConfigureAwait(false);
                    await RefreshCustomerFinancialsFromServerAsync(customerId, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeRepayment(true, false, projection, null, null);
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline)
            {
                return await EnqueueRepaymentCreateAsync(customerId, amount, remarks, customer, ct).ConfigureAwait(false);
            }

            return new ApiResultLikeRepayment(false, false, null, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline)
        {
            return new ApiResultLikeRepayment(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        if (customer is null)
        {
            return new ApiResultLikeRepayment(false, false, null, ApplicationErrorCodes.CustomerNotFound, "Customer was not found locally.");
        }

        return await EnqueueRepaymentCreateAsync(customerId, amount, remarks, customer, ct).ConfigureAwait(false);
    }

    public async Task<ApiResultLikeCredit> ReverseCreditAsync(
        Guid customerId,
        Guid creditEntryId,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ApiResultLikeCredit(false, false, null, "reason_required", "Reversal reason is required.");
        }

        var credit = contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
            ? await store.GetCreditAsync(creditEntryId, ct).ConfigureAwait(false)
            : null;

        if (credit is not null && credit.EntityState != LocalEntitySyncState.ServerConfirmed)
        {
            return new ApiResultLikeCredit(false, false, credit, "credit_not_confirmed", "Credit must be server-confirmed before reversal.");
        }

        if (credit?.EntityState == LocalEntitySyncState.PendingReversal)
        {
            return new ApiResultLikeCredit(false, false, credit, "reversal_already_pending", "A reversal is already pending for this credit.");
        }

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            var opId = Guid.NewGuid();
            var payload = JsonSerializer.Serialize(new { creditEntryId, customerId, reason }, JsonOptions);
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                Sha256Hex(payload),
                opId,
                OfflineOperationTypes.CreditReverse);

            var result = await api.ReverseCreditEntryAsync(
                    customerId,
                    creditEntryId,
                    new ReversePosCreditEntryRequest(reason),
                    headers,
                    ct)
                .ConfigureAwait(false);

            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapCredit(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerCreditAsync(projection, ct).ConfigureAwait(false);
                    await RefreshCustomerFinancialsFromServerAsync(customerId, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeCredit(true, false, projection, null, null);
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline && credit is not null)
            {
                return await EnqueueCreditReverseAsync(customerId, creditEntryId, reason, ct).ConfigureAwait(false);
            }

            return new ApiResultLikeCredit(false, false, credit, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline || credit is null)
        {
            return new ApiResultLikeCredit(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        return await EnqueueCreditReverseAsync(customerId, creditEntryId, reason, ct).ConfigureAwait(false);
    }

    public async Task<ApiResultLikeRepayment> ReverseRepaymentAsync(
        Guid repaymentId,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ApiResultLikeRepayment(false, false, null, "reason_required", "Reversal reason is required.");
        }

        LocalRepaymentProjection? repayment = null;
        if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
        {
            repayment = await store.GetRepaymentAsync(repaymentId, ct).ConfigureAwait(false);
        }

        if (repayment is not null && repayment.EntityState != LocalEntitySyncState.ServerConfirmed)
        {
            return new ApiResultLikeRepayment(false, false, repayment, "repayment_not_confirmed", "Repayment must be server-confirmed before reversal.");
        }

        if (repayment?.EntityState == LocalEntitySyncState.PendingReversal)
        {
            return new ApiResultLikeRepayment(false, false, repayment, "reversal_already_pending", "A reversal is already pending for this repayment.");
        }

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            var opId = Guid.NewGuid();
            var customerId = repayment?.CustomerId ?? Guid.Empty;
            var payload = JsonSerializer.Serialize(new { repaymentId, customerId, reason }, JsonOptions);
            var headers = new PosMutationIdempotencyHeaders(
                opId.ToString("N"),
                Sha256Hex(payload),
                opId,
                OfflineOperationTypes.RepaymentReverse);

            var result = await api.ReverseRepaymentAsync(
                    repaymentId,
                    new ReversePosRepaymentRequest(reason),
                    headers,
                    ct)
                .ConfigureAwait(false);

            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapRepayment(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerRepaymentAsync(projection, ct).ConfigureAwait(false);
                    await RefreshCustomerFinancialsFromServerAsync(result.Data.CustomerId, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeRepayment(true, false, projection, null, null);
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline && repayment is not null)
            {
                return await EnqueueRepaymentReverseAsync(repayment, reason, ct).ConfigureAwait(false);
            }

            return new ApiResultLikeRepayment(false, false, repayment, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline || repayment is null)
        {
            return new ApiResultLikeRepayment(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        return await EnqueueRepaymentReverseAsync(repayment, reason, ct).ConfigureAwait(false);
    }

    public async Task<ApiResultLikeCredit> SetCreditDueDateAsync(
        Guid creditEntryId,
        DateOnly? dueDate,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ApiResultLikeCredit(false, false, null, "reason_required", "Change reason is required.");
        }

        var credit = contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready
            ? await store.GetCreditAsync(creditEntryId, ct).ConfigureAwait(false)
            : null;

        if (credit is not null && credit.EntityState is not (LocalEntitySyncState.ServerConfirmed or LocalEntitySyncState.PendingUpdate or LocalEntitySyncState.Conflict))
        {
            if (credit.EntityState != LocalEntitySyncState.ServerConfirmed)
            {
                return new ApiResultLikeCredit(false, false, credit, "credit_not_confirmed", "Credit must be server-confirmed before due-date changes.");
            }
        }

        var isClear = dueDate is null;
        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (online)
        {
            var opId = Guid.NewGuid();
            var opType = isClear ? OfflineOperationTypes.CreditDueDateClear : OfflineOperationTypes.CreditDueDateSet;
            var payload = JsonSerializer.Serialize(new
            {
                creditEntryId,
                customerId = credit?.CustomerId,
                dueDate = dueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                reason,
                isClear,
                expectedCurrentDueDate = credit?.CurrentDueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }, JsonOptions);
            var headers = new PosMutationIdempotencyHeaders(opId.ToString("N"), Sha256Hex(payload), opId, opType);

            ApiResult<PosCreditEntryDto> result;
            if (isClear)
            {
                result = await api.ClearCreditDueDateAsync(
                        creditEntryId,
                        new ClearPosCreditDueDateRequest(reason, credit?.CurrentDueDate),
                        headers,
                        ct)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await api.SetCreditDueDateAsync(
                        creditEntryId,
                        new SetPosCreditDueDateRequest(dueDate, reason, credit?.CurrentDueDate),
                        headers,
                        ct)
                    .ConfigureAwait(false);
            }

            if (result.IsSuccess && result.Data is not null)
            {
                var projection = MapCredit(result.Data, LocalEntitySyncState.ServerConfirmed);
                if (contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
                {
                    await store.UpsertServerCreditAsync(projection, ct).ConfigureAwait(false);
                }

                return new ApiResultLikeCredit(true, false, projection, null, null);
            }

            if (result.Status == ApiCallStatus.Conflict && credit is not null
                && contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready)
            {
                await store.MarkCreditStateAsync(
                        creditEntryId,
                        LocalEntitySyncState.Conflict,
                        ApplicationErrorCodes.ConcurrencyConflict,
                        ct)
                    .ConfigureAwait(false);
                return new ApiResultLikeCredit(
                    false,
                    false,
                    credit,
                    ApplicationErrorCodes.ConcurrencyConflict,
                    result.Error?.Detail ?? "Due date was changed elsewhere.");
            }

            if (ShouldFallbackOffline(result.Status) && CanMutateOffline && credit is not null)
            {
                return await EnqueueDueDateAsync(credit, dueDate, reason, isClear, ct).ConfigureAwait(false);
            }

            return new ApiResultLikeCredit(false, false, credit, result.Error?.ErrorCode, result.Error?.Detail);
        }

        if (!CanMutateOffline || credit is null || credit.EntityState != LocalEntitySyncState.ServerConfirmed)
        {
            return new ApiResultLikeCredit(false, false, null, "offline_mutations_unavailable", "Reconnect to verify access");
        }

        return await EnqueueDueDateAsync(credit, dueDate, reason, isClear, ct).ConfigureAwait(false);
    }

    public async Task RefreshCustomerFinancialsFromServerAsync(Guid customerId, CancellationToken ct = default)
    {
        if (contextManager.ActiveContext?.Status != LocalContextInitStatus.Ready)
        {
            return;
        }

        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (!online)
        {
            await store.RebuildOptimisticBalancesAsync(customerId, ct).ConfigureAwait(false);
            return;
        }

        var summary = await api.GetCreditSummaryAsync(customerId, ct).ConfigureAwait(false);
        if (summary.IsSuccess && summary.Data is not null)
        {
            await store.SetConfirmedOutstandingAsync(customerId, summary.Data.OutstandingAmount, ct).ConfigureAwait(false);
        }

        await store.RebuildOptimisticBalancesAsync(customerId, ct).ConfigureAwait(false);
        syncStatus.Refresh();
    }

    private async Task<ApiResultLikeRepayment> EnqueueRepaymentCreateAsync(
        Guid customerId,
        decimal amount,
        string? remarks,
        LocalCustomerProjection? customer,
        CancellationToken ct)
    {
        Guid? dependsOnCustomer = null;
        Guid? dependsOnCredit = null;
        if (customer?.EntityState is LocalEntitySyncState.PendingCreate or LocalEntitySyncState.Syncing
            && customer.PendingOperationId is Guid pendingCustomerOp)
        {
            dependsOnCustomer = pendingCustomerOp;
        }

        var credits = await store.ListCreditsAsync(customerId, ct).ConfigureAwait(false);
        var pendingCredit = credits.FirstOrDefault(c =>
            c.EntityState is LocalEntitySyncState.PendingCreate or LocalEntitySyncState.Syncing
            && c.PendingOperationId is not null);
        if (pendingCredit?.PendingOperationId is Guid pendingCreditOp && dependsOnCustomer is null)
        {
            dependsOnCredit = pendingCreditOp;
        }

        var repaymentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        try
        {
            await store.PersistRepaymentCreateAndEnqueueAsync(
                    new LocalRepaymentCreateCommand(
                        repaymentId,
                        customerId,
                        operationId,
                        operationId.ToString("N"),
                        amount,
                        remarks,
                        dependsOnCustomer,
                        dependsOnCredit),
                    ct)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("local_overpayment", StringComparison.Ordinal))
        {
            return new ApiResultLikeRepayment(false, false, null, "local_overpayment", "Amount exceeds projected outstanding.");
        }

        syncStatus.Refresh();
        var created = await store.GetRepaymentAsync(repaymentId, ct).ConfigureAwait(false);
        return new ApiResultLikeRepayment(true, true, created, null, null);
    }

    private async Task<ApiResultLikeCredit> EnqueueCreditReverseAsync(
        Guid customerId,
        Guid creditEntryId,
        string reason,
        CancellationToken ct)
    {
        var operationId = Guid.NewGuid();
        await store.PersistCreditReverseAndEnqueueAsync(
                new LocalCreditReverseCommand(
                    creditEntryId,
                    customerId,
                    operationId,
                    operationId.ToString("N"),
                    reason),
                ct)
            .ConfigureAwait(false);
        syncStatus.Refresh();
        var credit = await store.GetCreditAsync(creditEntryId, ct).ConfigureAwait(false);
        return new ApiResultLikeCredit(true, true, credit, null, null);
    }

    private async Task<ApiResultLikeRepayment> EnqueueRepaymentReverseAsync(
        LocalRepaymentProjection repayment,
        string reason,
        CancellationToken ct)
    {
        var operationId = Guid.NewGuid();
        await store.PersistRepaymentReverseAndEnqueueAsync(
                new LocalRepaymentReverseCommand(
                    repayment.RepaymentId,
                    repayment.CustomerId,
                    operationId,
                    operationId.ToString("N"),
                    reason),
                ct)
            .ConfigureAwait(false);
        syncStatus.Refresh();
        var updated = await store.GetRepaymentAsync(repayment.RepaymentId, ct).ConfigureAwait(false);
        return new ApiResultLikeRepayment(true, true, updated, null, null);
    }

    private async Task<ApiResultLikeCredit> EnqueueDueDateAsync(
        LocalCreditProjection credit,
        DateOnly? dueDate,
        string reason,
        bool isClear,
        CancellationToken ct)
    {
        var operationId = Guid.NewGuid();
        await store.PersistCreditDueDateAndEnqueueAsync(
                new LocalCreditDueDateCommand(
                    credit.CreditEntryId,
                    credit.CustomerId,
                    operationId,
                    operationId.ToString("N"),
                    dueDate,
                    reason,
                    isClear,
                    credit.CurrentDueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ct)
            .ConfigureAwait(false);
        syncStatus.Refresh();
        var updated = await store.GetCreditAsync(credit.CreditEntryId, ct).ConfigureAwait(false);
        return new ApiResultLikeCredit(true, true, updated, null, null);
    }

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
        var customerIds = new HashSet<Guid>();
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
                customerIds.Add(item.CustomerId);
                var stamp = item.ReversedAtUtc ?? item.CreatedAtUtc;
                if (maxUtc is null || stamp > maxUtc)
                {
                    maxUtc = stamp;
                }
            }

            if (result.Data.Items.Count < result.Data.PageSize || result.Data.Items.Count == 0)
            {
                break;
            }

            page++;
        }

        await RebuildLocalBalancesAsync(customerIds, ct).ConfigureAwait(false);

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
            null,
            dto.CurrentDueDate);

    private static LocalRepaymentProjection MapRepayment(PosRepaymentDto dto, LocalEntitySyncState state) =>
        new(
            dto.RepaymentId,
            dto.CustomerId,
            dto.OrganizationId,
            dto.Amount,
            dto.Remarks,
            dto.Status,
            dto.RecordedAtUtc,
            state,
            null,
            null,
            null);

    private async Task DownloadRepaymentsAsync(CancellationToken ct)
    {
        var since = await store.GetDownloadCheckpointAsync("repayments", ct).ConfigureAwait(false);
        var page = 1;
        DateTimeOffset? maxUtc = since;
        var customerIds = new HashSet<Guid>();
        while (true)
        {
            var result = await api.SyncRepaymentsAsync(since, page, 100, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null)
            {
                break;
            }

            foreach (var item in result.Data.Items)
            {
                await store.UpsertServerRepaymentAsync(MapRepayment(item, LocalEntitySyncState.ServerConfirmed), ct)
                    .ConfigureAwait(false);
                customerIds.Add(item.CustomerId);
                var stamp = item.ReversedAtUtc ?? item.RecordedAtUtc;
                if (maxUtc is null || stamp > maxUtc)
                {
                    maxUtc = stamp;
                }
            }

            if (result.Data.Items.Count < result.Data.PageSize || result.Data.Items.Count == 0)
            {
                break;
            }

            page++;
        }

        await RebuildLocalBalancesAsync(customerIds, ct).ConfigureAwait(false);

        if (maxUtc is not null)
        {
            await store.SetDownloadCheckpointAsync("repayments", maxUtc.Value, ct).ConfigureAwait(false);
        }
    }

    private async Task RebuildLocalBalancesAsync(IEnumerable<Guid> customerIds, CancellationToken ct)
    {
        foreach (var customerId in customerIds)
        {
            await store.RebuildOptimisticBalancesAsync(customerId, ct).ConfigureAwait(false);
        }
    }

    private static string Sha256Hex(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
