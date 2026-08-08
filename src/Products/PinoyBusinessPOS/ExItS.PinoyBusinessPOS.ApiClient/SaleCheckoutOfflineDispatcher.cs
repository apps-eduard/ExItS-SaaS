using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Dispatches queued offline cash checkout operations. Server idempotency uses the durable SaleId.
/// </summary>
public sealed class SaleCheckoutOfflineDispatcher(
    IPosSaleClient client,
    ILocalCashSaleStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.SaleCheckout, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        CheckoutSaleRequest payload;
        try
        {
            payload = JsonSerializer.Deserialize<CheckoutSaleRequest>(plaintextPayload.Span, JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (!string.Equals(payload.PaymentMethod, PosSaleOptions.CashPaymentMethod, StringComparison.Ordinal))
        {
            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Permanent,
                "offline_payment_method_unsupported",
                null,
                null);
        }

        // Ensure SaleId is present for server idempotency replay.
        if (payload.SaleId is null || payload.SaleId == Guid.Empty)
        {
            if (envelope.EntityId is Guid entitySaleId)
            {
                payload = payload with { SaleId = entitySaleId };
            }
            else
            {
                return new OfflineDispatchResult(
                    false,
                    OfflineFailureClass.Permanent,
                    "payload_sale_id_missing",
                    null,
                    null);
            }
        }

        var result = await client.CheckoutAsync(payload, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore
                    .MarkSyncedAsync(payload.SaleId!.Value, result.Data.SaleId.ToString("D"), ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.SaleId.ToString("D"));
        }

        var failureClass = Classify(result.Status);
        var code = result.Error?.ErrorCode ?? result.Status.ToString();
        if (localStore is not null
            && failureClass is OfflineFailureClass.Permanent or OfflineFailureClass.Conflict)
        {
            await localStore.MarkSyncFailedAsync(payload.SaleId!.Value, code, ct).ConfigureAwait(false);
        }

        return new OfflineDispatchResult(false, failureClass, code, result.Error?.Detail, null);
    }

    private static OfflineFailureClass Classify(ApiCallStatus status) =>
        status switch
        {
            ApiCallStatus.Conflict => OfflineFailureClass.Conflict,
            ApiCallStatus.Validation => OfflineFailureClass.Permanent,
            ApiCallStatus.Forbidden => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.Unauthorized => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.NotFound => OfflineFailureClass.Permanent,
            ApiCallStatus.Offline => OfflineFailureClass.Transient,
            ApiCallStatus.Unavailable => OfflineFailureClass.Transient,
            ApiCallStatus.Timeout => OfflineFailureClass.Transient,
            _ => OfflineFailureClass.Transient
        };
}
