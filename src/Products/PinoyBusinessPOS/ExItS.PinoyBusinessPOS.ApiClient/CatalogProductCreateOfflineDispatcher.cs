using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Syncs an offline org-created product (metadata first), then uploads a pending custom photo if present.
/// Image bytes stay in private app files, never SQLite.
/// </summary>
public sealed class CatalogProductCreateOfflineDispatcher(
    IPosCatalogClient client,
    PendingProductImageStore pendingImages) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CatalogProductCreate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        CreatePosCatalogProductRequest payload;
        try
        {
            payload = JsonSerializer.Deserialize<CreatePosCatalogProductRequest>(plaintextPayload.Span, JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        var productId = payload.ProductId ?? Guid.Empty;
        if (productId == Guid.Empty)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        var created = await client
            .CreateProductAsync(payload with { ProductId = productId }, ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Data is null)
        {
            return MapFailure(created);
        }

        var pending = await pendingImages
            .TryReadAsync(envelope.OrganizationId, productId, ct)
            .ConfigureAwait(false);
        if (pending is { Length: > 0 })
        {
            var uploaded = await client
                .UploadProductImageAsync(created.Data.ProductId, pending, "image.jpg", ct)
                .ConfigureAwait(false);
            if (!uploaded.IsSuccess)
            {
                return MapFailure(uploaded);
            }
        }

        pendingImages.Delete(envelope.OrganizationId, productId);
        return new OfflineDispatchResult(
            true,
            OfflineFailureClass.None,
            null,
            null,
            created.Data.ProductId.ToString("D"));
    }

    private static OfflineDispatchResult MapFailure<T>(ApiResult<T> result)
    {
        if (result.Status == ApiCallStatus.Conflict)
        {
            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Conflict,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode ?? 409);
        }

        if (result.Status is ApiCallStatus.Validation or ApiCallStatus.NotFound or ApiCallStatus.Forbidden)
        {
            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Permanent,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode);
        }

        return new OfflineDispatchResult(
            false,
            OfflineFailureClass.Transient,
            result.Error?.ErrorCode ?? result.Status.ToString(),
            result.Error?.Detail,
            null,
            result.Error?.StatusCode);
    }
}
