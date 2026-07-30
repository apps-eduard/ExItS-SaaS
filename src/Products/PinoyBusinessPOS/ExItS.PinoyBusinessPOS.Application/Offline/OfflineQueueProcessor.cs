using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
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

        syncStatus.SetReconnectRequired(false);

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
    IConnectivityService connectivity) : IOfflineAccessRevalidator
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
}

/// <summary>Development/Testing probe operation type constant.</summary>
public static class OfflineOperationTypes
{
    public const string DevOfflineProbe = "dev.offline-probe";
}
