using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// FIFO offline queue processor with access revalidation, decryption, dispatch, and retry classification.
/// </summary>
public sealed class OfflineQueueProcessor(
    IOfflineOperationQueue queue,
    ILocalPayloadProtector payloadProtector,
    IOfflineAccessRevalidator accessRevalidator,
    IOfflineRetryClassifier retryClassifier,
    IEnumerable<IOfflineOperationDispatcher> dispatchers,
    ILocalContextManager contextManager,
    IPosSyncStatusService syncStatus,
    TimeProvider? timeProvider = null) : IOfflineQueueProcessor
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public async Task<OfflineProcessBatchResult> ProcessAvailableAsync(CancellationToken ct = default)
    {
        if (contextManager.ActiveContext is null)
        {
            return new OfflineProcessBatchResult(0, 0, 0, "SyncStatus_Offline");
        }

        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            syncStatus.SetRecoveryRequired(true);
            syncStatus.Refresh();
            return new OfflineProcessBatchResult(0, 0, 0, "SyncStatus_KeyUnavailable");
        }

        await queue.RecoverAbandonedSyncingAsync(ct).ConfigureAwait(false);

        var access = await accessRevalidator.RevalidateAsync(ct).ConfigureAwait(false);
        if (!access.Allowed)
        {
            // Mark currently claimable items as blocked without deleting them.
            var blocked = 0;
            while (true)
            {
                var claimed = await queue.TryClaimNextAsync(Guid.NewGuid().ToString("N"), ct).ConfigureAwait(false);
                if (claimed is null)
                {
                    break;
                }

                await queue.MarkFailureAsync(
                        claimed.OperationId,
                        OfflineFailureClass.AccessBlocked,
                        access.ReasonCode ?? "access_blocked",
                        "Reconnect to verify access",
                        nextAttemptUtc: _clock.GetUtcNow().AddMinutes(5),
                        attemptCount: claimed.AttemptCount,
                        ct)
                    .ConfigureAwait(false);
                blocked++;
            }

            syncStatus.SetReconnectRequired(true);
            syncStatus.Refresh();
            return new OfflineProcessBatchResult(blocked, 0, blocked, "SyncStatus_Reconnect");
        }

        // Access restored: return previously blocked work to Pending (never discard).
        await queue.ReclaimBlockedByAccessAsync(ct).ConfigureAwait(false);

        syncStatus.SetReconnectRequired(false);
        syncStatus.SetRecoveryRequired(false);

        var processed = 0;
        var succeeded = 0;
        var failed = 0;
        var claimToken = Guid.NewGuid().ToString("N");

        // Process one at a time to preserve FIFO and avoid overtaking.
        var envelope = await queue.TryClaimNextAsync(claimToken, ct).ConfigureAwait(false);
        while (envelope is not null)
        {
            processed++;
            syncStatus.Refresh();

            try
            {
                var opAccess = await accessRevalidator
                    .RevalidateOperationAsync(envelope.OperationType, ct)
                    .ConfigureAwait(false);
                if (!opAccess.Allowed)
                {
                    failed++;
                    await queue.MarkFailureAsync(
                            envelope.OperationId,
                            OfflineFailureClass.AccessBlocked,
                            opAccess.ReasonCode ?? "capability_denied",
                            "Access or capability denied",
                            nextAttemptUtc: _clock.GetUtcNow().AddMinutes(5),
                            attemptCount: envelope.AttemptCount,
                            ct)
                        .ConfigureAwait(false);
                }
                else
                {
                    var loaded = await queue.TryLoadEncryptedAsync(envelope.OperationId, ct).ConfigureAwait(false);
                    if (loaded is null)
                    {
                        failed++;
                        await queue.MarkFailureAsync(
                                envelope.OperationId,
                                OfflineFailureClass.Permanent,
                                "payload_missing",
                                null,
                                nextAttemptUtc: null,
                                attemptCount: envelope.AttemptCount,
                                ct)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var contextHash = contextManager.ActiveContext!.Identity.ContextHash;
                        var aad = OfflinePayloadBinding.BuildAssociatedData(
                            contextHash,
                            envelope.OperationId,
                            envelope.OperationType);

                        byte[] plaintext;
                        try
                        {
                            plaintext = await payloadProtector.DecryptAsync(loaded.Value.Encrypted, aad, ct)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            failed++;
                            await queue.MarkFailureAsync(
                                    envelope.OperationId,
                                    OfflineFailureClass.Permanent,
                                    "payload_decrypt_failed",
                                    null,
                                    nextAttemptUtc: null,
                                    attemptCount: envelope.AttemptCount,
                                    ct)
                                .ConfigureAwait(false);
                            plaintext = [];
                        }

                        if (plaintext.Length > 0)
                        {
                            var dispatcher = dispatchers.FirstOrDefault(d => d.CanHandle(envelope.OperationType));
                            if (dispatcher is null)
                            {
                                failed++;
                                await queue.MarkFailureAsync(
                                        envelope.OperationId,
                                        OfflineFailureClass.Permanent,
                                        "handler_missing",
                                        envelope.OperationType,
                                        nextAttemptUtc: null,
                                        attemptCount: envelope.AttemptCount,
                                        ct)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                var result = await dispatcher.DispatchAsync(envelope, plaintext, ct).ConfigureAwait(false);
                                if (result.Succeeded)
                                {
                                    succeeded++;
                                    await queue.MarkSucceededAsync(envelope.OperationId, result.ServerReference, ct)
                                        .ConfigureAwait(false);
                                    await queue.SetLastSyncedUtcAsync(_clock.GetUtcNow(), ct).ConfigureAwait(false);
                                }
                                else
                                {
                                    failed++;
                                    await ApplyFailureAsync(envelope, result, ct).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                // Avoid retaining plaintext references longer than needed.
                syncStatus.Refresh();
            }

            envelope = await queue.TryClaimNextAsync(claimToken, ct).ConfigureAwait(false);
        }

        syncStatus.Refresh();
        return new OfflineProcessBatchResult(processed, succeeded, failed, null);
    }

    private async Task ApplyFailureAsync(
        OfflineOperationEnvelope envelope,
        OfflineDispatchResult result,
        CancellationToken ct)
    {
        var failureClass = result.FailureClass;
        if (failureClass == OfflineFailureClass.None)
        {
            failureClass = OfflineFailureClass.Permanent;
        }

        if (failureClass == OfflineFailureClass.Transient
            && envelope.AttemptCount >= retryClassifier.MaxAttempts)
        {
            failureClass = OfflineFailureClass.Permanent;
        }

        DateTimeOffset? next = null;
        if (failureClass == OfflineFailureClass.Transient)
        {
            next = retryClassifier.ComputeNextAttemptUtc(envelope.AttemptCount, _clock.GetUtcNow());
        }
        else if (failureClass == OfflineFailureClass.AccessBlocked)
        {
            next = _clock.GetUtcNow().AddMinutes(5);
            syncStatus.SetReconnectRequired(true);
        }

        await queue.MarkFailureAsync(
                envelope.OperationId,
                failureClass,
                result.FailureCode ?? "dispatch_failed",
                result.FailureSummary,
                next,
                envelope.AttemptCount,
                ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Revalidates session access using the current authenticated session flags (online required by shell policy).</summary>
public sealed class OfflineAccessRevalidator(
    ICurrentUserContext currentUser,
    IProtectedShellAccessPolicy accessPolicy,
    IConnectivityService connectivity,
    IUtangCapabilityEvaluator? capabilities = null) : IOfflineAccessRevalidator
{
    public async Task<OfflineAccessRevalidationResult> RevalidateAsync(CancellationToken ct = default)
    {
        await accessPolicy.InitializeAsync(ct).ConfigureAwait(false);
        var online = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        if (!online)
        {
            return new OfflineAccessRevalidationResult(false, "offline");
        }

        if (!currentUser.IsAuthenticated
            || currentUser.Session?.OrganizationId is null
            || !currentUser.HasPosAccess)
        {
            return new OfflineAccessRevalidationResult(false, "access_denied");
        }

        if (!accessPolicy.CanEnterProtectedShell)
        {
            return new OfflineAccessRevalidationResult(false, "reconnect_required");
        }

        return new OfflineAccessRevalidationResult(true, null);
    }

    public async Task<OfflineAccessRevalidationResult> RevalidateOperationAsync(
        string operationType,
        CancellationToken ct = default)
    {
        var baseAccess = await RevalidateAsync(ct).ConfigureAwait(false);
        if (!baseAccess.Allowed)
        {
            return baseAccess;
        }

        if (capabilities is null
            || string.Equals(operationType, OfflineOperationTypes.DevOfflineProbe, StringComparison.Ordinal))
        {
            return baseAccess;
        }

        if (!TryMapCapability(operationType, out var capability))
        {
            // Unknown business operation types fail closed.
            return new OfflineAccessRevalidationResult(false, "capability_unknown");
        }

        if (!capabilities.IsAllowed(capability))
        {
            return new OfflineAccessRevalidationResult(false, "capability_denied");
        }

        return new OfflineAccessRevalidationResult(true, null);
    }

    private static bool TryMapCapability(string operationType, out UtangCapability capability)
    {
        capability = default;
        if (string.Equals(operationType, OfflineOperationTypes.CustomerCreate, StringComparison.Ordinal))
        {
            capability = UtangCapability.CreateCustomer;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.CustomerUpdate, StringComparison.Ordinal))
        {
            capability = UtangCapability.EditCustomer;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.CreditCreate, StringComparison.Ordinal))
        {
            capability = UtangCapability.CreateCredit;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.RepaymentCreate, StringComparison.Ordinal))
        {
            capability = UtangCapability.RecordRepayment;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.RepaymentReverse, StringComparison.Ordinal))
        {
            capability = UtangCapability.ReverseRepayment;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.CreditReverse, StringComparison.Ordinal))
        {
            capability = UtangCapability.ReverseCredit;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.CreditDueDateSet, StringComparison.Ordinal)
            || string.Equals(operationType, OfflineOperationTypes.CreditDueDateClear, StringComparison.Ordinal))
        {
            capability = UtangCapability.MutateDueDate;
            return true;
        }

        if (string.Equals(operationType, OfflineOperationTypes.SaleCheckout, StringComparison.Ordinal))
        {
            capability = UtangCapability.CreateSale;
            return true;
        }

        return false;
    }
}

/// <summary>Generic offline operation type constants (one queue for all types).</summary>
public static class OfflineOperationTypes
{
    public const string DevOfflineProbe = "dev.offline-probe";
    public const string CustomerCreate = "customer.create";
    public const string CustomerUpdate = "customer.update";
    public const string CreditCreate = "credit.create";
    public const string RepaymentCreate = "repayment.create";
    public const string RepaymentReverse = "repayment.reverse";
    public const string CreditReverse = "credit.reverse";
    public const string CreditDueDateSet = "credit.due-date.set";
    public const string CreditDueDateClear = "credit.due-date.clear";

    /// <summary>
    /// Cash sale checkout — used for server idempotency online and for encrypted outbox dispatch
    /// of offline cash sales. Non-cash payment methods must not be queued.
    /// </summary>
    public const string SaleCheckout = "sale.checkout";

    /// <summary>
    /// Server-side idempotency scope for electronic payment attempt create. Online-only.
    /// </summary>
    public const string PaymentAttemptCreate = "payment.attempt.create";

    /// <summary>
    /// Server-side idempotency operation type for expense create. Expenses are online-only: no offline
    /// dispatcher, queue handler, or local projection exists for this type. It names the server
    /// idempotency scope so a retried create request replays instead of double-recording.
    /// </summary>
    public const string ExpenseCreate = "expense.create";

    /// <summary>
    /// Server-side idempotency operation type for purchase order submit. Purchasing is online-only.
    /// </summary>
    public const string PurchaseOrderSubmit = "purchase_order.submit";

    /// <summary>
    /// Server-side idempotency operation type for purchase order receive. Purchasing is online-only.
    /// </summary>
    public const string PurchaseOrderReceive = "purchase_order.receive";

    /// <summary>Server-side idempotency operation type for cashier shift cash movements. Online-only.</summary>
    public const string CashierShiftMovement = "cashier_shift.movement";

    /// <summary>Server-side idempotency operation type for sale returns. Online-only.</summary>
    public const string SaleReturnCreate = "sale_return.create";

    /// <summary>Server-side idempotency for POS role assignment. Online-only.</summary>
    public const string PosRoleAssign = "pos_role.assign";

    /// <summary>Server-side idempotency for POS role revocation. Online-only.</summary>
    public const string PosRoleRevoke = "pos_role.revoke";
}
